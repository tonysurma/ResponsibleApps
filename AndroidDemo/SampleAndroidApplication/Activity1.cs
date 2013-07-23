using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Akavache;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

namespace SampleAndroidApplication
{
    [Activity(Label = "SampleAndroidApplication", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : ListActivity
    {
        IList<TaskItem> _taskList = new List<TaskItem>();
        WebRequestTimeOut _connectionTimer;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var inflater = MenuInflater;
            inflater.Inflate(Resource.Menu.Options, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_request:
                    var key = "request-" + Guid.NewGuid().ToString();
                    BlobCache.LocalMachine.InsertObject(key, new Request() { Key = key });
                    ProcessRequests();
                    return true;
                case Resource.Id.menu_refresh:
                    RefreshTasks();
                    return true;
                case Resource.Id.menu_httpclient:
                    MakeAsyncHttpClientRequest();
                    return true;
                case Resource.Id.menu_async:
                    MakeAsynchronusRequest();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
        }

        #region HttpMethods

        private async void MakeAsyncHttpClientRequest()
        {
            try
            {

                UpdateConnectionStatus("Initiating Connection", true);

                HttpClientHandler handler = new HttpClientHandler();

                // Configure compression
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                // Create a New HttpClient object.
                HttpClient client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromMilliseconds(20000);

                HttpResponseMessage response = await client.GetAsync("http://www.theverge.com");

                if (!response.IsSuccessStatusCode)
                {
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);

            }
            catch (TaskCanceledException ex)
            {
                UpdateConnectionStatus("Request Timed Out");
            }
            catch (Exception e)
            {
                UpdateConnectionStatus("Ex: " + e.Message);
            }
        }

        void MakeAsynchronusRequest()
        {
            var request = HttpWebRequest.Create("http://www.microsoft.com/");
            request.Method = "GET";
            UpdateConnectionStatus("Initiating Connection", true);

            //doesn't exist in silverlight
            //  request.Timeout = 20000;
            _connectionTimer = new WebRequestTimeOut(request, 20000);


            request.BeginGetResponse(HandleResponse, request);
            _connectionTimer.StartTimer();
        }

        void HandleResponse(IAsyncResult result)
        {
            if (_connectionTimer.TimedOut)
            {
                UpdateConnectionStatus("Request Timed Out");
            }
            else
            {
                var request = (HttpWebRequest)result.AsyncState;
                try
                {
                    using (var response = (HttpWebResponse)request.EndGetResponse(result))
                    {

                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string content = reader.ReadToEnd();
                            UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                        }
                    }
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                }
            }
        }

        void UpdateConnectionStatus(string text, bool inprogress = false)
        {
            //Dispatcher.BeginInvoke(() =>
            //{
            //    App.ViewModel.UpdateItem(0, text);
            //    SystemTray.ProgressIndicator.IsVisible = inprogress;
            //});

            List<Tuple<string, TaskItem>> tasks = new List<Tuple<string, TaskItem>>();

            // fake up a task with update info
            TaskItem updateTask = new TaskItem();
            updateTask.Title = text;
            updateTask.CreatedDate = DateTime.Now;

            tasks.Add(new Tuple<string, TaskItem>(text, updateTask));

            RunOnUiThread(() => { ListAdapter = new SimpleListItem2_Adapter(this, tasks); });
        }

        void MakeSynchronousRequest()
        {
            var request = HttpWebRequest.Create("http://www.microsoft.com/");
            request.Method = "GET";
            UpdateConnectionStatus("Initiating Connection");

            _connectionTimer = new WebRequestTimeOut(request, 2000);

            var handle = request.BeginGetResponse(HandleResponse, request);
            _connectionTimer.StartTimer();

            //this won't work in wp8/silverlight (unsupportedexception) but here for illustration
            handle.AsyncWaitHandle.WaitOne(2000);

            if (_connectionTimer.TimedOut)
            {

                UpdateConnectionStatus("Request Timed Out");

            }
            else
            {
                try
                {
                    using (var response = (HttpWebResponse)request.EndGetResponse(handle))
                    {

                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string content = reader.ReadToEnd();
                            UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                        }
                    }
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                }
            }
        }

        #endregion

        #region Caching Support

        void RefreshTasks()
        {
            //get and fetch from akavache list of tasks
            //will refresh first with cached list of tasks (if any) and again with fetched

            BlobCache.LocalMachine.GetAndFetchLatest("TaskList", RetrieveLatestTasks()).Subscribe(l =>
            {
                Console.WriteLine("Got TaskList");
                _taskList = l;
                UpdateTaskListUI();
            });
        }

        Func<IObservable<IList<TaskItem>>> RetrieveLatestTasks()
        {
            return () => System.Reactive.Linq.Observable.Start(() => LatestTasks());
        }

        static IList<TaskItem> LatestTasks()
        {
            var result = new List<TaskItem>();
            for (int i = 0; i < 5; i++)
            {
                result.Add(NewTask(i.ToString(), 1000));
            }
            return result;
        }

        static TaskItem NewTask(string suffix = "", int delay = 0)
        {
            Thread.Sleep(delay);
            return new TaskItem
            {
                Title = "TaskItem" + suffix + " @ " + DateTime.Now.ToShortTimeString(),
                Description = "Yet another task item",
                CreatedDate = DateTime.Now
            };
        }

        #endregion

        #region storeforward

        private void ProcessRequests()
        {
            BlobCache.LocalMachine.GetAllObjects<Request>().Subscribe(pending =>
            {
                foreach (var request in pending)
                {
                    //if successful remove from cache
                    if (MakeHttpCallForRequest(request))
                    {
                        BlobCache.LocalMachine.Invalidate(request.Key);
                    }
                }
            });
        }

        private bool MakeHttpCallForRequest(Request request)
        {
            //make http call
            Thread.Sleep(1000);
            return true;
        }

        #endregion

        void UpdateTaskListUI()
        {
            List<Tuple<string, TaskItem>> tasks = new List<Tuple<string, TaskItem>>();

            foreach (TaskItem item in _taskList)
            {
                tasks.Add(new Tuple<string, TaskItem>(Guid.NewGuid().ToString(), item));
            }

            RunOnUiThread(() => { ListAdapter = new SimpleListItem2_Adapter(this, tasks); });
        }

        Func<IObservable<TaskItem>> DelayedTask()
        {
            return () => System.Reactive.Linq.Observable.Start(() => NewTask("delay", 12000));
        }
    }

    public sealed class Task
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class Request
    {
        public string Key { get; set; }
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}


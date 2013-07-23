using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using PivotAppHttpAsync.Resources;

using Akavache;

using PivotAppHttpAsync.ViewModels;

namespace PivotAppHttpAsync {
    public partial class MainPage : PhoneApplicationPage {
        WebRequestTimeOut _connectionTimer;

        // Constructor
        public MainPage() {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            if (!App.ViewModel.IsDataLoaded) {
                App.ViewModel.LoadData();
            }


            ProgressIndicator progress = SystemTray.ProgressIndicator;
            if (progress == null) {
                progress = new ProgressIndicator();
                SystemTray.ProgressIndicator = progress;
            }
            progress.IsIndeterminate = true;
            progress.IsVisible = false;

            BlobCache.ApplicationName = "WP8ConnRespDemo";

            Microsoft.Phone.Reactive.Observable.Start(ProcessRequests);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            BlobCache.Shutdown();
        }

        #region HttpMethods

        private async void MakeAsyncHttpClientRequest() {
            try {

                App.ViewModel.ClearList();
                UpdateConnectionStatus("Initiating Connection", true);

                HttpClientHandler handler = new HttpClientHandler();

                // Configure compression
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                // Create a New HttpClient object.
                HttpClient client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromMilliseconds(20000);

                HttpResponseMessage response = await client.GetAsync("http://www.theverge.com");

                if (!response.IsSuccessStatusCode) {
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);

            } catch (TaskCanceledException ex) {
                UpdateConnectionStatus("Request Timed Out");
            }
            catch (Exception e) {
                UpdateConnectionStatus("Ex: " + e.Message);
            }
        }

        void MakeAsynchronusRequest() {
            var request = HttpWebRequest.Create("http://www.microsoft.com/");
            request.Method = "GET";

            App.ViewModel.ClearList();
            UpdateConnectionStatus("Initiating Connection", true);

            //doesn't exist in silverlight
            //  request.Timeout = 20000;
            _connectionTimer = new WebRequestTimeOut(request, 20000);


            request.BeginGetResponse(HandleResponse, request);
            _connectionTimer.StartTimer();
        }

        void HandleResponse(IAsyncResult result) {
            if (_connectionTimer.TimedOut) {
                UpdateConnectionStatus("Request Timed Out");
            } else {
                var request = (HttpWebRequest)result.AsyncState;
                try {
                    using (var response = (HttpWebResponse)request.EndGetResponse(result)) {

                        using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                            string content = reader.ReadToEnd();
                            UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                        }
                    }
                } catch (WebException ex) {
                    var response = ex.Response as HttpWebResponse;
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                }
            }
        }

        void UpdateConnectionStatus(string text, bool inprogress = false) {
            Dispatcher.BeginInvoke(() => {
                App.ViewModel.UpdateItem(0, text);
                SystemTray.ProgressIndicator.IsVisible = inprogress;
            });
        }

        void MakeSynchronousRequest() {
            var request = HttpWebRequest.Create("http://www.microsoft.com/");
            request.Method = "GET";

            App.ViewModel.ClearList();
            UpdateConnectionStatus("Initiating Connection");

            _connectionTimer = new WebRequestTimeOut(request, 2000);

            var handle = request.BeginGetResponse(HandleResponse, request);
            _connectionTimer.StartTimer();

            //this won't work in wp8/silverlight (unsupportedexception) but here for illustration
            handle.AsyncWaitHandle.WaitOne(2000);

            if (_connectionTimer.TimedOut) {

                UpdateConnectionStatus("Request Timed Out");

            } else {


                try {
                    using (var response = (HttpWebResponse)request.EndGetResponse(handle)) {

                        using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                            string content = reader.ReadToEnd();
                            UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                        }
                    }
                } catch (WebException ex) {
                    var response = ex.Response as HttpWebResponse;
                    UpdateConnectionStatus("Got: " + (int)response.StatusCode + " " + response.StatusCode);
                }
            }
        }

        #endregion

        private IList<TaskItem> _taskList = new List<TaskItem>();

        #region Caching Support
 
        void RefreshTasks() {
            //get and fetch from akavache list of tasks
            //will refresh first with cached list of tasks (if any) and again with fetched

            if (App.ViewModel.Items.Count < 5)
                App.ViewModel.ClearList();

            BlobCache.LocalMachine.GetAndFetchLatest("TaskList", RetrieveLatestTasks()).Subscribe(l => {
                Debug.WriteLine("Got TaskList");
                _taskList = l;
                UpdateTaskListUI();
            });
        }

        Func<IObservable<IList<TaskItem>>> RetrieveLatestTasks() {
            return () => Microsoft.Phone.Reactive.Observable.Start(() => LatestTasks());
        }

        static IList<TaskItem> LatestTasks() {
            var result = new List<TaskItem>();
            for (int i = 0; i < 5; i++) {
                result.Add(NewTask(i.ToString(), 1000));
            }
            return result;
        }

        static TaskItem NewTask(string suffix = "", int delay = 0) {
            Thread.Sleep(delay);
            return new TaskItem {
                Title = "TaskItem" + suffix + " @ " + DateTime.Now.ToShortTimeString(),
                Description = "Yet another task item",
                CreatedDate = DateTime.Now
            };
        }

        #endregion

        #region storeforward

        private void ProcessRequests() {
            BlobCache.LocalMachine.GetAllObjects<Request>().Subscribe(pending => {
                foreach (var request in pending) {
                    //if successful remove from cache
                    if (MakeHttpCallForRequest(request)) {
                        BlobCache.LocalMachine.Invalidate(request.Key);
                    }
                }
            });
        }

        private bool MakeHttpCallForRequest(Request request) {
            //make http call
            Thread.Sleep(1000);
            return true;
        }

        #endregion

        void UpdateTaskListUI() {
            this.Dispatcher.BeginInvoke(() => App.ViewModel.ReplaceWithTaskList(_taskList));
        }

        private void btnHttpClient_Click(object sender, EventArgs e) {
            MakeAsyncHttpClientRequest();
        }

        private void btnAsync_Click(object sender, EventArgs e) {
            MakeAsynchronusRequest();
        }

        private void btnRefresh_Click(object sender, EventArgs e) {
            RefreshTasks();
        }

        private void btnAddRequest_Click(object sender, EventArgs e) {
            var key = "request-" + Guid.NewGuid().ToString();
            BlobCache.LocalMachine.InsertObject(key, new Request() { Key = key });
            ProcessRequests();
        }


        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}
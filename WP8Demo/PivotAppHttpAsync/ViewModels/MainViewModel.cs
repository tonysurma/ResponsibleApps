using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using PivotAppHttpAsync.Resources;

namespace PivotAppHttpAsync.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        public MainViewModel() {
            this.Items = new ObservableCollection<ItemViewModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> Items { get; private set; }

        private string _sampleProperty = "Sample Runtime Property Value";
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string SampleProperty {
            get {
                return _sampleProperty;
            }
            set {
                if (value != _sampleProperty) {
                    _sampleProperty = value;
                    NotifyPropertyChanged("SampleProperty");
                }
            }
        }

        /// <summary>
        /// Sample property that returns a localized string
        /// </summary>
        public string LocalizedSampleProperty {
            get {
                return AppResources.SampleProperty;
            }
        }

        public bool IsDataLoaded {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData() {
            // Sample data; replace with real data
            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ClearList() {
            this.Items.Clear();
        }

        public void UpdateItem(int index, string title) {
            if (index >= this.Items.Count) {
                this.Items.Add(new ItemViewModel() {
                                                       LineOne = title,
                                                       LineTwo = DateTime.Now.ToString()
                                                   });
            } else {
                var item = this.Items[index];
                item.LineOne = title;
                item.LineTwo = DateTime.Now.ToString();
            }
        }

        public void ReplaceWithTaskList(IList<TaskItem> list) {
            for (int i = 0; i < list.Count; i++) {
                ReplaceWithTask(i, list[i]);
            }
        }

        public void ReplaceWithTask(int index, TaskItem task) {
            if (this.Items.Count < index + 1) {
                this.Items.Add(ItemFromTask(task));
            } else {
                this.Items[index] = ItemFromTask(task);
            }
        }

        static ItemViewModel ItemFromTask(TaskItem task) {
            return new ItemViewModel() {
                LineOne = task.Title,
                LineTwo = task.CreatedDate.ToString(),
                LineThree = task.Description
            };
        }
    }

    public class Request {
        public string Key { get; set; }
    }

    public class TaskItem {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
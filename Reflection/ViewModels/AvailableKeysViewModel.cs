using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class AvailableKeysViewModel {
        public ObservableCollection<UserKey> UserKeys { get; set; }
        public ObservableCollection<UserKey> SelectedKeys { get; set; }

        public AvailableKeysViewModel() {
            UserKeys = new ObservableCollection<UserKey>();
            SelectedKeys = new ObservableCollection<UserKey>();
            UserKeys.CollectionChanged += OnUserKeysCollectionChanged;
        }

        public void OnUserKeysCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (UserKey item in e.NewItems) {
                    item.PropertyChanged += OnCheckedPropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (UserKey item in e.OldItems) {
                    item.PropertyChanged -= OnCheckedPropertyChanged;
                }
            }
        }

        public void OnCheckedPropertyChanged(object sender, PropertyChangedEventArgs e) {
            var userKey = (UserKey)sender;
            if (userKey.IsChecked) {
                SelectedKeys.Add(userKey);
            } else {
                SelectedKeys.Remove(userKey);
            }
        }

    }
}

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
    public class ColumnNamesViewModel {
        public ObservableCollection<ColumnName> AvailableKeys { get; set; }
        public ObservableCollection<ColumnName> SelectedKeys { get; set; }

        public ColumnNamesViewModel() {
            AvailableKeys = new ObservableCollection<ColumnName>();
            SelectedKeys = new ObservableCollection<ColumnName>();
            AvailableKeys.CollectionChanged += OnAvailableKeysCollectionChanged;
        }

        public void OnAvailableKeysCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (ColumnName item in e.NewItems) {
                    item.PropertyChanged += OnCheckedPropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (ColumnName item in e.OldItems) {
                    item.PropertyChanged -= OnCheckedPropertyChanged;
                }
            }
        }

        public void OnCheckedPropertyChanged(object sender, PropertyChangedEventArgs e) {
            var userKey = (ColumnName)sender;
            if (userKey.IsChecked) {
                SelectedKeys.Add(userKey);
            } else {
                SelectedKeys.Remove(userKey);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class ColumnNamesViewModel {
        public string Name { get; set; }
        public ObservableCollection<ColumnName> AvailableKeys { get; set; }
        public ICollectionView FilteredAvailableKeys { get; private set; }
        public ObservableCollection<ColumnName> SelectedKeys { get; set; }
        public List<int> UnAvailableKeys { get; set; }

        public ColumnNamesViewModel(string name) {
            Name = name;
            AvailableKeys = new ObservableCollection<ColumnName>();
            FilteredAvailableKeys = CollectionViewSource.GetDefaultView(AvailableKeys);
            SelectedKeys = new ObservableCollection<ColumnName>();
            UnAvailableKeys = new List<int>();
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
                var prevUserKey = SelectedKeys.Where(item => item.Id == userKey.Id).FirstOrDefault();
                SelectedKeys.Remove(prevUserKey);
                SelectedKeys.Add(userKey);
            } else {
                SelectedKeys.Remove(userKey);
            }
        }

    }
}

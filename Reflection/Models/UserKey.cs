using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class UserKey : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Id { get; set; }
        public string Key { get; set; }
        bool isChecked;
        public bool IsChecked {
            get { return isChecked; }
            set {
                if (isChecked != value) {
                    isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        public UserKey(int id, string key) {
            Id = id;
            Key = key;
            IsChecked = false;
        }

        public void OnPropertyChanged(string propName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}

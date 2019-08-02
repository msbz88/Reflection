using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ColumnName : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public int Id { get; set; }
        public string Value { get; set; }
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

        public ColumnName(int id, string value) {
            Id = id;
            Value = value;
            IsChecked = false;
        }

        public void OnPropertyChanged(string propName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}

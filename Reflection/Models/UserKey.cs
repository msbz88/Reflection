using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class UserKey {
        public int Id { get; set; }
        public string Key { get; set; }
        public bool IsChecked { get; set; }

        public UserKey(int id, string key) {
            Id = id;
            Key = key;
            IsChecked = false;
        }
    }
}

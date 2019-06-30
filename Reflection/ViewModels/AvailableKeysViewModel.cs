using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class AvailableKeysViewModel {
        public List<UserKey> UserKeys { get; set; }

        public AvailableKeysViewModel() {
            UserKeys = new List<UserKey>();
        }
    }
}

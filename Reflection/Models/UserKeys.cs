using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class UserKeys {
        public List<int> UserComparisonKeys { get; set; }
        public List<int> UserIdColumns { get; set; }
        public List<int> UserExcludeColumns { get; set; }

        public UserKeys() {
            UserComparisonKeys = new List<int>();
            UserIdColumns = new List<int>();
            UserExcludeColumns = new List<int>();
        }
    }
}

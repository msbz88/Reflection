using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class UserKeys {
        public HashSet<int> UserComparisonKeys { get; set; }
        public HashSet<int> UserIdColumns { get; set; }
        public HashSet<int> UserExcludeColumns { get; set; }

        public UserKeys() {
            UserComparisonKeys = new HashSet<int>();
            UserIdColumns = new HashSet<int>();
            UserExcludeColumns = new HashSet<int>();
        }
    }
}

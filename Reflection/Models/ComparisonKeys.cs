using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class ComparisonKeys {
        public HashSet<int> MainKeys { get; set; }
        public HashSet<int> SingleIdColumns { get; set; }
        public HashSet<int> BinaryIdColumns { get; set; }
        public HashSet<int> ExcludeColumns { get; set; }

        public ComparisonKeys() {
            MainKeys = new HashSet<int>();
            SingleIdColumns = new HashSet<int>();
            BinaryIdColumns = new HashSet<int>();
            ExcludeColumns = new HashSet<int>();
        }
    }
}

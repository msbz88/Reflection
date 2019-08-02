using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class ComparisonKeys {
        public List<int> MainKeys { get; set; }
        public List<int> TransactionKeys { get; set; }
        public List<int> ExcludeColumns { get; set; }
        public List<int> UserKeys { get; set; }

        public ComparisonKeys() {
            MainKeys = new List<int>();
            TransactionKeys = new List<int>();
            ExcludeColumns = new List<int>();
            UserKeys = new List<int>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class ComparisonKeys {
        public List<int> MainKeys { get; set; }
        public List<int> TransactionKeys { get; set; } = new List<int>();
        public List<int> ExcludeColumns { get; set; } = new List<int>();
    }
}

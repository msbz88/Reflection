using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class ComparisonKeys {
        public List<int> MainKeys { get; set; }
        public List<int> AdditionalKeys { get; set; } = new List<int>();
    }
}

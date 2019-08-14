using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
   public class ComparisonKeys {
        public List<int> MainKeys { get; set; }
        public List<int> BinaryValues { get; set; }
        public List<int> ExcludeColumns { get; set; }
        public List<int> UserKeys { get; set; }
        public List<int> UserIdColumns { get; set; }
        public List<int> UserExcludeColumns { get; set; }
        public List<int> UserExcludeColumnsBinary { get; set; }

        public ComparisonKeys() {
            MainKeys = new List<int>();
            BinaryValues = new List<int>();
            ExcludeColumns = new List<int>();
            UserKeys = new List<int>();
            UserIdColumns = new List<int>();
            UserExcludeColumns = new List<int>();
            UserExcludeColumnsBinary = new List<int>();
        }
    }
}

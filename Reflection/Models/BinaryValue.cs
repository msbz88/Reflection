using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class BinaryValue {
        public int ColumnId { get; set; } = -1;
        public string MasterValue { get; set; }
        public string TestValue { get; set; }
    }
}

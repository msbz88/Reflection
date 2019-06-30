using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class PrintIdFields {
        public int AdditionalKey { get; set; } = -1;
        public string MasterAdditionalKeyVal { get; set; }
        public string TestAdditionalKeyVal { get; set; }
        public int MainKey { get; set; } = -1;
        public string MainVal { get; set; }
    }
}

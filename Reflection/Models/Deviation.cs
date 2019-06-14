using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class Deviation {       
        public int ColumnId { get; set; }
        public string MasterValue { get; set; }
        public string TestValue { get; set; }

        public Deviation(int columnId, string masterValue, string testValue) {          
            ColumnId = columnId;
            MasterValue = masterValue;
            TestValue = testValue;
        }
    }
}

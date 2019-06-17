using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection {
    public class IdField {
        public int ColumnId { get; set; }
        public string ColumnName { get; set; }
        public string Value { get; set; }

        public IdField(int columnId, string columnName, string value) {
            ColumnId = columnId;
            ColumnName = columnName;
            Value = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ComparedRow {
        public int MasterRowId { get; set; }
        public int TestRowId { get; set; }
        public bool IsPassed { get; set; }
        public List<BinaryValue> TransNoColumns { get; private set; }
        public Dictionary<int, string> IdColumns { get; private set; }
        public List<Deviation> Deviations { get; private set; }

        public ComparedRow(int masterRowId, int testRowId) {
            MasterRowId = masterRowId;
            TestRowId = testRowId;
            Deviations = new List<Deviation>();
            IdColumns = new Dictionary<int, string>();
            TransNoColumns = new List<BinaryValue>();
        }

        public void AddMainIdColumns(Dictionary<int, string> idColumns) {
            foreach (var item in idColumns) {
                IdColumns.Add(item.Key, item.Value);
            }
        }

        public void AddTransNoColumns(List<BinaryValue> transNoColumns) {
            TransNoColumns.AddRange(transNoColumns);
        }

        public void AddDeviation(Deviation deviation) {
            Deviations.Add(deviation);
        }

        //public override string ToString() {
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(MasterRowId);
        //    sb.Append(";");
        //    sb.Append(TestRowId);
        //    foreach (var item in IdColumns) {
        //        sb.Append(";");
        //        sb.Append(item.MainVal);
        //    }
        //    foreach (var item in Deviations) {
        //        sb.Append(";");
        //        sb.Append(item.MasterValue + " | " + item.TestValue);
        //    }
        //    return sb.ToString();
        //}

    }
}

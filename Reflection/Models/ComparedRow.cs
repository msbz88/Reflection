using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ComparedRow {
        public int MasterRowId { get; set; }
        public int TestRowId { get; set; }
        public Dictionary<int, string> IdFields { get; private set; }
        public List<Deviation> Deviations { get; private set; }

        public ComparedRow(int masterRowId, int testRowId) {
            MasterRowId = masterRowId;
            TestRowId = testRowId;
            Deviations = new List<Deviation>();
            IdFields = new Dictionary<int, string>();
        }

        public void AddDeviation(Deviation deviation) {
            Deviations.Add(deviation);
        }

        public void AddIdFields(Dictionary<int, string> idFields) {
            IdFields = idFields;
        }

    }
}

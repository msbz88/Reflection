using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class KnownDefect {
        public string Project { get; set; }
        public double LowerVersion { get; set; }
        public double UpperVersion { get; set; }
        public string DefectNo { get; set; }
        public string MasterTransNo { get; set; }
        public string TestTransNo { get; set; }
        public string SecId { get; set; }
        public string DeviationColumnName { get; set; }
        public string MasterValue { get; set; }
        public string TestValue { get; set; }
        public DateTime ChangedDate { get; set; }

        public KnownDefect(string project, double lowerVersion, double upperVersion, string defectNo, string masterTransNo, string testTransNo, string secId, string deviationColumnName, string masterValue, string testValue, DateTime changedDate) {
            Project = project.Trim();
            LowerVersion = lowerVersion;
            UpperVersion = upperVersion;
            DefectNo = defectNo.Trim();
            MasterTransNo = masterTransNo.Trim();
            TestTransNo = testTransNo.Trim();
            SecId = secId.Trim();
            DeviationColumnName = deviationColumnName.Trim();
            MasterValue = masterValue.Trim();
            TestValue = testValue.Trim();
            ChangedDate = changedDate;
        }
    }
}

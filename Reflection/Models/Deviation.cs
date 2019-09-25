using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Reflection.Models {
    public class Deviation {       
        public int ColumnId { get; set; }
        public string MasterValue { get; set; }
        public string TestValue { get; set; }
        public double? Difference { get; set; }

        public Deviation(int columnId, string masterValue, string testValue) {          
            ColumnId = columnId;
            MasterValue = masterValue;
            TestValue = testValue;
        }

        public void CalculateDiff(bool isString) {
            if (isString) {
                Difference = LevenshteinDistance();
            }else {
                var dMaster = ConvertToDouble(MasterValue);
                var dTest = ConvertToDouble(TestValue);
                Difference = dMaster > dTest ? dMaster - dTest : dTest - dMaster;
            }
        }

        private int LevenshteinDistance(){
            var source1Length = MasterValue.Length;
            var source2Length = TestValue.Length;
            var matrix = new int[source1Length + 1, source2Length + 1];
            if (source1Length == 0)
                return source2Length;
            if (source2Length == 0)
                return source1Length;
            for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }
            for (var i = 1; i <= source1Length; i++) {
                for (var j = 1; j <= source2Length; j++) {
                    var cost = (TestValue[j - 1] == MasterValue[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[source1Length, source2Length];
        }

        private double ConvertToDouble(string numberString) {
            var cleanedString = numberString.Replace(" ", "");
            double d = 0;
            var culture = Helpers.SetCultureInfo(cleanedString);
            double.TryParse(cleanedString, NumberStyles.AllowDecimalPoint, provider: culture, result: out d);
            return d;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ColumnSummary {
        private int UniqCount { get; set; }
        public int UniqMatchCount { get; set; }
        public int ColumnId { get; set; }
        ///<summary>Percentage of equal values</summary>
        public double MatchingRate { get; set; }
        ///<summary>Percentage of distinct values</summary>
        public double UniquenessRate { get; set; }
        ///<summary>Percentage of equal values to total values in column</summary>
        public double UniqMatchRate { get; set; }
        public bool IsString { get; set; }
        public bool IsDouble { get; set; }
        public bool HasNulls { get; set; }
        private bool IsNumber { get; set; }

        public ColumnSummary(int id, int totalRowsCount, HashSet<string> masterUniqVals, HashSet<string> testUniqVals) {
            UniqMatchCount = masterUniqVals.Intersect(testUniqVals).Count();
            UniqCount = masterUniqVals.Count > testUniqVals.Count ? testUniqVals.Count : masterUniqVals.Count;
            ColumnId = id;
            MatchingRate = CalculateRate(masterUniqVals.Count, testUniqVals.Count, UniqMatchCount);
            UniquenessRate = CalculatePercentage(UniqCount, totalRowsCount);
            UniqMatchRate = CalculatePercentage(UniqMatchCount, totalRowsCount);
            HasNulls = CheckIfHasNulls(masterUniqVals);
            IsNumber = CheckIfNumeric(masterUniqVals);
            IsDouble = IsNumber ? false : CheckIfDouble(masterUniqVals);
            IsString = IsDouble || IsNumber ? false : true;
        }

        private bool CheckIfDouble(HashSet<string> columnData) {
            double d;
            var clearSeq = columnData.Where(item => item != "" && item.ToUpper() != "NULL");
            return clearSeq.Any() ? clearSeq.All(item => double.TryParse(item, out d)) : false;
        }

        private bool CheckIfHasNulls(HashSet<string> columnData) {
            return columnData.Any(item => item == "" || item.ToUpper() == "NULL");
        }

        private bool CheckIfNumeric(HashSet<string> columnData) {
            int n = 0;
            long l = 0;
            var clearSeq = columnData.Where(item => item != "" && item.ToUpper() != "NULL");
            return clearSeq.Any() ? clearSeq.All(item => int.TryParse(item, out n) || long.TryParse(item, out l)) : false;
        }

        private double CalculatePercentage(int x, int y) {
            return Math.Round(((double)x / y) * 100, 2);
        }

        private double CalculateRate(int uniqueRowsMaster, int uniqueRowsTest, int matchedValues) {
            if (matchedValues == 0) {
                return 0;
            } else {
                double finalRate = 0;
                var lowerNumber = uniqueRowsMaster > uniqueRowsTest ? uniqueRowsMaster : uniqueRowsTest;
                finalRate = ((double)matchedValues / lowerNumber) * 100;
                return Math.Round(finalRate, 2);
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(ColumnId);
            sb.Append(";");
            sb.Append(MatchingRate);
            sb.Append(";");
            sb.Append(UniquenessRate);
            sb.Append(";");
            sb.Append(UniqMatchRate);
            sb.Append(";");
            sb.Append(IsDouble);
            sb.Append(";");
            sb.Append(IsString);
            sb.Append(";");
            sb.Append(HasNulls);
            return sb.ToString();
        }
    }
}

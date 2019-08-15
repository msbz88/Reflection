using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class Comparator {
         ComparisonKeys ComparisonKeys { get; set; }
        List<int> ExcludedColumns { get; set; }

        public Comparator(ComparisonKeys comparisonKeys) {
            ComparisonKeys = comparisonKeys;
            ExcludedColumns = new List<int>();
            ExcludedColumns.AddRange(comparisonKeys.BinaryValues);
            ExcludedColumns.AddRange(comparisonKeys.ExcludeColumns);
            ExcludedColumns.AddRange(comparisonKeys.UserExcludeColumns);
            ExcludedColumns = ExcludedColumns.Distinct().ToList();
        }

        public ComparedRow Compare(List<ComparedRow> allCombinations, Row masterRow, Row testRow, ref int minDeviations) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            int currentDeviations = 0;
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (!ExcludedColumns.Contains(i)) {
                    if (masterRow.Data[i] != testRow.Data[i]) {
                        currentDeviations++;
                        if (currentDeviations <= minDeviations) {
                            var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                            comparedRow.AddDeviation(deviation);
                        } else {
                            return null;
                        }
                    }
                }
            }
            if (comparedRow.Deviations.Count > 0) {
                var prevBooked = allCombinations.Where(row => row.TestRowId == testRow.Id).FirstOrDefault();
                if (prevBooked != null) {
                    int countPrevResult = prevBooked.Deviations.Count;
                    if (countPrevResult > currentDeviations) {
                        allCombinations.Remove(prevBooked);
                    } else if (currentDeviations > countPrevResult) {
                        return null;
                    }
                }
                comparedRow.AddTransNoColumns(GetTransNoColumns(masterRow, testRow));
                comparedRow.AddMainIdColumns(GetMainIdColumns(masterRow, testRow));
                minDeviations = currentDeviations;
                return comparedRow;
            } else {
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        public ComparedRow CompareSingle(Row masterRow, Row testRow) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (!ExcludedColumns.Contains(i)) {
                    if (masterRow.Data[i] != testRow.Data[i]) {
                        var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                        comparedRow.AddDeviation(deviation);
                    }
                }
            }
            if (comparedRow.Deviations.Count > 0) {
                comparedRow.AddTransNoColumns(GetTransNoColumns(masterRow, testRow));
                comparedRow.AddMainIdColumns(GetMainIdColumns(masterRow, testRow));
                return comparedRow;
            } else {
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        private Dictionary<int, string> GetMainIdColumns(Row masterRow, Row testRow) {
            Dictionary<int, string> mainColumnsId = new Dictionary<int, string>();
            foreach (var item in ComparisonKeys.MainKeys) {                
                mainColumnsId.Add(item, masterRow.Data[item]);
            }
            foreach (var item in ComparisonKeys.UserIdColumns) {
                if (!mainColumnsId.ContainsKey(item)) {
                    mainColumnsId.Add(item, masterRow.Data[item]);
                }                   
            }
            return mainColumnsId;
        }

        private List<BinaryValue> GetTransNoColumns(Row masterRow, Row testRow) {
            List<BinaryValue> transNoColumns = new List<BinaryValue>();
            foreach (var item in ComparisonKeys.BinaryValues) {
                BinaryValue transNo = new BinaryValue();
                transNo.ColumnId = item;
                transNo.MasterValue = masterRow.Data[item];
                transNo.TestValue = testRow.Data[item];
                transNoColumns.Add(transNo);
            }
            foreach (var item in ComparisonKeys.UserIdColumnsBinary) {
                if (transNoColumns.All(col => col.ColumnId != item)) {
                    BinaryValue userExcludeColumn = new BinaryValue();
                    userExcludeColumn.ColumnId = item;
                    userExcludeColumn.MasterValue = masterRow.Data[item];
                    userExcludeColumn.TestValue = testRow.Data[item];
                    transNoColumns.Add(userExcludeColumn);
                }
            }
            return transNoColumns;
        }
    }
}

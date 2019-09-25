using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class Comparator {
        ComparisonKeys ComparisonKeys { get; set; }
        HashSet<int> ExcludedColumns { get; set; }
        bool IsDeviationsOnly { get; set; }

        public Comparator(ComparisonKeys comparisonKeys, bool isDeviationsOnly) {
            IsDeviationsOnly = isDeviationsOnly;
            ComparisonKeys = comparisonKeys;
            ExcludedColumns = new HashSet<int>();
            ExcludedColumns = comparisonKeys.ExcludeColumns;
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
                if (IsBetterResultExists(allCombinations, testRow, currentDeviations)) {
                    return null;
                }
                comparedRow.AddTransNoColumns(GetTransNoColumns(masterRow, testRow));
                comparedRow.AddMainIdColumns(GetMainIdColumns(masterRow, testRow));
                minDeviations = currentDeviations;
                return comparedRow;
            } else {
                if (!IsDeviationsOnly) {
                    comparedRow.AddTransNoColumns(GetTransNoColumns(masterRow, testRow));
                    comparedRow.AddMainIdColumns(GetMainIdColumns(masterRow, testRow));
                }
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        private bool IsBetterResultExists(List<ComparedRow> allCombinations, Row testRow, int currentDeviations) {
            var prevBooked = allCombinations.Where(row => row.TestRowId == testRow.Id).ToList();
            foreach (var item in prevBooked) {
                if(item.Deviations.Count > currentDeviations) {
                    allCombinations.Remove(item);
                }else if(item.Deviations.Count < currentDeviations) {
                    return true;
                }
            }
            return false;
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
                if (!IsDeviationsOnly) {
                    comparedRow.AddTransNoColumns(GetTransNoColumns(masterRow, testRow));
                    comparedRow.AddMainIdColumns(GetMainIdColumns(masterRow, testRow));
                }
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        private Dictionary<int, string> GetMainIdColumns(Row masterRow, Row testRow) {
            Dictionary<int, string> mainColumnsId = new Dictionary<int, string>();
            foreach (var item in ComparisonKeys.MainKeys) {                
                mainColumnsId.Add(item, masterRow.Data[item]);
            }
            foreach (var item in ComparisonKeys.SingleIdColumns) {
                if (!mainColumnsId.ContainsKey(item)) {
                    mainColumnsId.Add(item, masterRow.Data[item]);
                }                   
            }
            return mainColumnsId;
        }

        private List<BinaryValue> GetTransNoColumns(Row masterRow, Row testRow) {
            List<BinaryValue> transNoColumns = new List<BinaryValue>();
            foreach (var item in ComparisonKeys.BinaryIdColumns) {
                BinaryValue transNo = new BinaryValue();
                transNo.ColumnId = item;
                transNo.MasterValue = masterRow.Data[item];
                transNo.TestValue = testRow.Data[item];
                transNoColumns.Add(transNo);
            }
            foreach (var item in ComparisonKeys.BinaryIdColumns) {
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

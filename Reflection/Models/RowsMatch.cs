using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class RowsMatch {
        private List<ColumnSummary> BaseStat { get; set; }
        List<ComparedRow> ComparedRows { get; set; }
        List<ComparedRow> AllCombinations { get; set; }
        List<int> IdColumns { get; set; }

        public RowsMatch(List<ColumnSummary> baseStat, List<int> idColumns) {
            BaseStat = baseStat;
            ComparedRows = new List<ComparedRow>();
            AllCombinations = new List<ComparedRow>();
            IdColumns = idColumns;
        }

        private List<ComparedRow> CreateAllCombinations(List<Row> masterRows, List<Row> testRows) {
            AllCombinations.Clear();
            foreach (var mRow in masterRows) {
                foreach (var tRow in testRows) {
                    AllCombinations.Add(Compare(mRow, tRow));
                }
            }
            return AllCombinations;
        }

        public List<ComparedRow> ProcessGroup(List<Row> masterRows, List<Row> testRows) {
            ComparedRows.Clear();
            CreateAllCombinations(masterRows, testRows);
            while (AllCombinations.Count > 0) {
                int minDeviation = AllCombinations.Min(row => row.Deviations.Count);
                var bestCombinations = AllCombinations.Where(row => row.Deviations.Count == minDeviation).ToList();
                if (bestCombinations.Count == 1) {
                    var comparedRow = bestCombinations.First();
                    ComparedRows.Add(comparedRow);
                    var wrongCombinations = AllCombinations.Where(row => row.MasterRowId == comparedRow.MasterRowId || row.TestRowId == comparedRow.TestRowId).ToList();
                    RemoveWrongCombinations(wrongCombinations);
                } else {
                    var deviationsColumns = bestCombinations.SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
                    var statDeviationsColumns = BaseStat.Where(stat => deviationsColumns.Contains(stat.ColumnId));
                    ComparedRow comparedRow = null;
                    if (deviationsColumns.Count == 1 && !statDeviationsColumns.First().IsString) {
                        var masterValue = bestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.MasterValue))).First();
                        var testValues = bestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.TestValue))).ToList();
                        var bestMatch = ClosestNumber(testValues, masterValue);
                        comparedRow = bestCombinations.Where(row => row.Deviations.Select(col => col.TestValue).Contains(bestMatch.ToString())).First();
                    } else if(deviationsColumns.Count == 1 && statDeviationsColumns.First().IsString) {                  
                        comparedRow = bestCombinations.OrderBy(row => row.Deviations.Select(col=>col.TestValue)).First();
                    } else {                      
                        var columnsOrderedByPriority = statDeviationsColumns.OrderBy(col => col.MatchingRate).ThenBy(col => col.UniqMatchRate).Select(col => col.ColumnId);
                        List<ComparedRow> bestMatched = new List<ComparedRow>();
                        foreach (var item in columnsOrderedByPriority) {
                            var firstMatched = bestCombinations.SelectMany(row=>row.Deviations.Where(col=>col.ColumnId==item).Select(col=>col.TestValue)).OrderBy(col=>col).First();
                            bestMatched = bestCombinations.Where(row => row.Deviations.Select(col => col.TestValue).Contains(firstMatched)).ToList();
                            if (bestMatched.Count == 1) {
                                comparedRow = bestMatched.First();
                                break;
                            }
                            bestCombinations = bestMatched;
                        }
                        if (bestMatched.Count > 1) {
                            comparedRow = bestMatched.First();
                        }
                    }
                    ComparedRows.Add(comparedRow);
                    var wrongCombinations = AllCombinations.Where(row => row.MasterRowId == comparedRow.MasterRowId || row.TestRowId == comparedRow.TestRowId).ToList();
                    RemoveWrongCombinations(wrongCombinations);
                }
            }
            return ComparedRows;
        }

        private double ConvertToDouble(string numberString) {
            double d = 0;
            double.TryParse(numberString, out d);
            return d;
        }

        private double ClosestNumber(List<double> columnValues, double compareTo) {
            return columnValues.Aggregate((x, y) => Math.Abs(x - compareTo) < Math.Abs(y - compareTo) ? x : y);
        }
       
        private void RemoveWrongCombinations(List<ComparedRow> wrongCombinations) {
            foreach (var item in wrongCombinations) {
                AllCombinations.Remove(item);
            }
        }

        private Dictionary<int, string> GetIdFields(Row masterRow, Row testRow) {
            var mId = masterRow.ColumnIndexIn(IdColumns);
            var tId = testRow.ColumnIndexIn(IdColumns);
            Dictionary<int, string> idFields =new Dictionary<int, string>();
            for (int i = 0; i < mId.Count;i++) {
                if (masterRow.Data[i] == testRow.Data[i]) {
                    idFields.Add(i, masterRow.Data[i]);
                }          
            }
            return idFields;
        }
        
        public ComparedRow Compare(Row masterRow, Row testRow) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (masterRow.Data[i] != testRow.Data[i]) {
                   var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                   comparedRow.AddDeviation(deviation);
                   comparedRow.AddIdFields(GetIdFields(masterRow, testRow));
                }
            }
            return comparedRow;
        }
    }
}


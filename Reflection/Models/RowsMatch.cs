using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class RowsMatch {
        private List<ColumnSummary> BaseStat { get; set; }
        List<ComparedRow> ComparedRows { get; set; }
        List<ComparedRow> AllCombinations { get; set; }
        List<int> IdColumns { get; set; }
        ComparisonTask ComparisonTask { get; set; }
        int columnsCount = 0;

        public RowsMatch(List<ColumnSummary> baseStat, List<int> idColumns, ComparisonTask comparisonTask) {
            BaseStat = baseStat;
            ComparedRows = new List<ComparedRow>();
            AllCombinations = new List<ComparedRow>();
            IdColumns = idColumns;
            ComparisonTask = comparisonTask;
            columnsCount = baseStat.Select(item => item.ColumnId).Distinct().Count();
        }

        private List<ComparedRow> CreateAllCombinations(List<Row> masterRows, List<Row> testRows) {
            AllCombinations.Clear();
            foreach (var mRow in masterRows) {
                int minDeviations = columnsCount;
                foreach (var tRow in testRows) {
                    var comparedRow = Compare(mRow, tRow, ref minDeviations);
                    if (comparedRow != null) {
                        AllCombinations.Add(comparedRow);
                    }
                }
            }
            return AllCombinations;
        }

        public List<ComparedRow> ProcessGroup(List<Row> masterRows, List<Row> testRows, int allGroups) {
            ComparedRows.Clear();
            CreateAllCombinations(masterRows, testRows);
            while (AllCombinations.Count > 0) {
                int minDeviation = AllCombinations.Min(row => row.Deviations.Count);
                //doesnot filter all values == minDeviation
                var bestCombinations = AllCombinations.Where(row => row.Deviations.Count == minDeviation).ToList();
                //File.WriteAllLines(@"C: \Users\MSBZ\Desktop\allComb.txt", AllCombinations.Select(row => row.ToString()));
                //File.WriteAllLines(@"C: \Users\MSBZ\Desktop\bestComb.txt", bestCombinations.Select(row => row.ToString()));
                if (bestCombinations.Count == 1) {
                    var comparedRow = bestCombinations.First();
                    ComparedRows.Add(comparedRow);
                    RemoveWrongCombinations(comparedRow);
                } else {
                    var deviationsColumns = bestCombinations.SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
                    if (deviationsColumns.Count == minDeviation) {
                        var masterGroup = bestCombinations.GroupBy(item => item.MasterRowId).Where(item => item.Count() > 1).Select(item => item.Key);
                        var testGroup = bestCombinations.GroupBy(item => item.TestRowId).Where(item => item.Count() > 1).Select(item => item.Key);
                        var comparedRows = bestCombinations.Where(item => !masterGroup.Contains(item.MasterRowId) && !testGroup.Contains(item.TestRowId)).ToList();
                        if (comparedRows.Count > 0) {
                            ComparedRows.AddRange(comparedRows);
                            foreach (var compRow in comparedRows) {
                                RemoveWrongCombinations(compRow);
                            }
                            continue;
                        }
                    }
                    var statDeviationsColumns = BaseStat.Where(stat => deviationsColumns.Contains(stat.ColumnId));
                    ComparedRow comparedRow = null;
                    if (deviationsColumns.Count == 1 && !statDeviationsColumns.First().IsString) {
                        var masterValue = bestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.MasterValue))).First();
                        var testValues = bestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.TestValue))).ToList();
                        var bestMatch = ClosestNumber(testValues, masterValue);
                        comparedRow = bestCombinations.Where(row => row.Deviations.Select(col => ConvertToDouble(col.TestValue)).Contains(bestMatch)).First();
                    } else if (deviationsColumns.Count == 1 && statDeviationsColumns.First().IsString) {
                        comparedRow = bestCombinations.OrderBy(row => row.Deviations.Select(col => col.TestValue)).First();
                    } else {
                        var columnsOrderedByPriority = statDeviationsColumns.OrderBy(col => col.MatchingRate).ThenBy(col => col.UniqMatchRate).Select(col => col.ColumnId);
                        List<ComparedRow> bestMatched = new List<ComparedRow>();
                        foreach (var item in columnsOrderedByPriority) {
                            var firstMatched = bestCombinations.SelectMany(row => row.Deviations.Where(col => col.ColumnId == item).Select(col => col.TestValue)).OrderBy(val => val).FirstOrDefault();
                            if (string.IsNullOrEmpty(firstMatched)) {
                                bestMatched.Add(bestCombinations.First());
                                break;
                            }
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
                    RemoveWrongCombinations(comparedRow);
                }
            }
            ComparisonTask.Progress += 20 / (double)allGroups;
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

        private void RemoveWrongCombinations(ComparedRow comparedRow) {
            var wrongCombinations = AllCombinations.Where(row => row.MasterRowId == comparedRow.MasterRowId || row.TestRowId == comparedRow.TestRowId).ToList();
            foreach (var item in wrongCombinations) {
                AllCombinations.Remove(item);
            }
        }

        private Dictionary<int, string> GetIdFields(Row masterRow, Row testRow) {
            Dictionary<int, string> idFields = new Dictionary<int, string>();
            foreach (var item in IdColumns) {
                idFields.Add(item, masterRow.Data[item]);
            }
            return idFields;
        }

        public ComparedRow Compare(Row masterRow, Row testRow, ref int minDeviations) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            int currentDeviations = 0;
            for (int i = 0; i < masterRow.Data.Length; i++) {
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
            var prevBooked = AllCombinations.Where(row => row.TestRowId == testRow.Id).FirstOrDefault();
            if (prevBooked != null) {
                int countPrevResult = prevBooked.Deviations.Count;
                if (countPrevResult > currentDeviations) {
                    AllCombinations.Remove(prevBooked);
                } else if(currentDeviations > countPrevResult) {
                    return null;
                }
            }
            comparedRow.AddIdFields(GetIdFields(masterRow, testRow));
            minDeviations = currentDeviations;
            return comparedRow;
        }

        public ComparedRow CompareSingle(Row masterRow, Row testRow) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (masterRow.Data[i] != testRow.Data[i]) {
                    var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                    comparedRow.AddDeviation(deviation);
                }
            }
            comparedRow.AddIdFields(GetIdFields(masterRow, testRow));
            return comparedRow;
        }

        private void RemoveWithMoreDeviations(ComparedRow comparedRow) {
            var wrongCombinations = AllCombinations.Where(row => row.MasterRowId == comparedRow.MasterRowId && row.Deviations.Count > comparedRow.Deviations.Count).ToList();
            foreach (var item in wrongCombinations) {
                AllCombinations.Remove(item);
            }
        }

    }
}


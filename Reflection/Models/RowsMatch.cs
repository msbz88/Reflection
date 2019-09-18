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
        List<ComparedRow> BestCombinations { get; set; }
        ComparisonTask ComparisonTask { get; set; }
        Comparator Comparator { get; set; }
        int columnsCount = 0;

        public RowsMatch(List<ColumnSummary> baseStat, ComparisonKeys idColumns, ComparisonTask comparisonTask) {
            BaseStat = baseStat;
            ComparedRows = new List<ComparedRow>();
            AllCombinations = new List<ComparedRow>();
            ComparisonTask = comparisonTask;
            columnsCount = baseStat.Select(item => item.ColumnId).Distinct().Count();
            Comparator = new Comparator(idColumns, ComparisonTask.IsDeviationsOnly);
            BestCombinations = new List<ComparedRow>();
        }

        private List<ComparedRow> CreateAllCombinations(List<Row> masterRows, List<Row> testRows) {
            AllCombinations.Clear();
            foreach (var mRow in masterRows) {
                ComparisonTask.IfCancelRequested();
                int minDeviations = columnsCount;
                foreach (var tRow in testRows) {
                    ComparisonTask.IfCancelRequested();
                    var comparedRow = Comparator.Compare(AllCombinations, mRow, tRow, ref minDeviations);
                    if (comparedRow != null) {
                        if (comparedRow.IsPassed) {
                            ComparedRows.Add(comparedRow);
                            var delRow = testRows.Where(item => item.Id == comparedRow.TestRowId).FirstOrDefault();
                            testRows.Remove(delRow);
                            RemoveWrongCombination(comparedRow);
                            break;
                        } else {
                            AllCombinations.Add(comparedRow);
                        }                       
                    }
                }
            }
            return AllCombinations;
        }

        public List<ComparedRow> ProcessGroup(List<Row> masterRows, List<Row> testRows, int allGroups) {
            ComparedRows.Clear();
            CreateAllCombinations(masterRows, testRows);
            //FindPassedRows();
            while (AllCombinations.Count > 0) {
                ComparisonTask.IfCancelRequested();
                int minDeviation = AllCombinations.Min(row => row.Deviations.Count);
                BestCombinations.Clear();
                BestCombinations.AddRange(AllCombinations.Where(row => row.Deviations.Count == minDeviation).ToList());
                ComparedRow comparedRow = null;
                if (BestCombinations.Count == 1) {
                    comparedRow = BestCombinations.First();
                    AddComparedRow(comparedRow);
                    continue;
                }
                //deviations in same columns
                var deviationsColumns = BestCombinations.SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
                var statDeviationsColumns = BaseStat.Where(stat => deviationsColumns.Contains(stat.ColumnId));
                if (deviationsColumns.Count == minDeviation) {
                    List<ComparedRow> comparedRows = HandleDeviationsInSameColumns();
                    if(comparedRows.Count > 0) {
                        foreach (var item in comparedRows) {
                            AddComparedRow(item);
                        }
                    }else {
                        comparedRow = SortByPriority(statDeviationsColumns);
                        AddComparedRow(comparedRow);
                    }
                    continue;
                }
                //deviations in different columns
                comparedRow = HandleDeviationsInDiffColumns(deviationsColumns);
                AddComparedRow(comparedRow);
            }
            ComparisonTask.UpdateProgress(17 / (double)allGroups);
            return ComparedRows;
        }

        private void FindPassedRows() {
            var passedRows = AllCombinations.Where(item => item.IsPassed);
            ComparedRows.AddRange(passedRows);
            RemoveAllPassedCombinations();
        }

        private void AddComparedRow(ComparedRow comparedRow) {
            ComparedRows.Add(comparedRow);
            RemoveWrongCombination(comparedRow);
        }

        private List<ComparedRow> HandleDeviationsInSameColumns() {
            var masterGroup = BestCombinations.GroupBy(item => item.MasterRowId).Where(item => item.Count() > 1).Select(item => item.Key);
            var testGroup = BestCombinations.GroupBy(item => item.TestRowId).Where(item => item.Count() > 1).Select(item => item.Key);
            return BestCombinations.Where(item => !masterGroup.Contains(item.MasterRowId) && !testGroup.Contains(item.TestRowId)).ToList();
        }

        private ComparedRow HandleDeviationsInDiffColumns(List<int> deviationsColumns) {
            var statDeviationsColumns = BaseStat.Where(stat => deviationsColumns.Contains(stat.ColumnId));
            ComparedRow comparedRow = null;
            if (deviationsColumns.Count == 1 && !statDeviationsColumns.First().IsString) {
                var masterValue = BestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.MasterValue))).First();
                var testValues = BestCombinations.SelectMany(row => row.Deviations.Select(col => ConvertToDouble(col.TestValue))).ToList();
                var bestMatch = ClosestNumber(testValues, masterValue);
                comparedRow = BestCombinations.Where(row => row.Deviations.Select(col => ConvertToDouble(col.TestValue)).Contains(bestMatch)).First();
            } else if (deviationsColumns.Count == 1 && statDeviationsColumns.First().IsString) {
                comparedRow = BestCombinations.OrderBy(row => row.Deviations.Select(col => col.TestValue).First()).First();
            } else {
                comparedRow = SortByPriority(statDeviationsColumns);
            }
            return comparedRow;
        }

        private ComparedRow SortByPriority(IEnumerable<ColumnSummary> statDeviationsColumns) {
            var columnsOrderedByPriority = statDeviationsColumns.Where(col => col.UniquenessRate > 1).OrderBy(col => col.MatchingRate).ThenBy(col => col.UniqMatchRate).Select(col => col.ColumnId);
            List<ComparedRow> bestMatched = new List<ComparedRow>();
            ComparedRow comparedRow = null;
            foreach (var item in columnsOrderedByPriority) {
                ComparisonTask.IfCancelRequested();
                var firstMatched = BestCombinations.SelectMany(row => row.Deviations.Where(col => col.ColumnId == item).Select(col => col.TestValue)).OrderBy(val => val).FirstOrDefault();
                if (string.IsNullOrEmpty(firstMatched)) {
                    continue;
                }
                bestMatched = BestCombinations.Where(row => row.Deviations.Select(col => col.TestValue).Contains(firstMatched)).ToList();
                if (bestMatched.Count == 1) {
                    comparedRow = bestMatched.First();
                    return comparedRow;
                }
                if(BestCombinations.Count > bestMatched.Count) {
                    BestCombinations.Clear();
                    BestCombinations.AddRange(bestMatched);
                }
            }
            comparedRow = bestMatched.First();
            return comparedRow;
        }

        private double ConvertToDouble(string numberString) {
            double d = 0;
            double.TryParse(numberString, out d);
            return d;
        }

        private double ClosestNumber(List<double> columnValues, double compareTo) {
            return columnValues.Aggregate((x, y) => Math.Abs(x - compareTo) < Math.Abs(y - compareTo) ? x : y);
        }

        private void RemoveWrongCombination(ComparedRow comparedRow) {
            if (comparedRow != null) {
                AllCombinations.RemoveAll(row => row.MasterRowId == comparedRow.MasterRowId || row.TestRowId == comparedRow.TestRowId);
            }
        }

        private void RemoveAllPassedCombinations() {
            AllCombinations.RemoveAll(row => row.IsPassed);
        }

    }
}


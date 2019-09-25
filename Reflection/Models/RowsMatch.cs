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
                var deviationsColumns = BestCombinations.SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
                var statDeviationsColumns = BaseStat.Where(stat => deviationsColumns.Contains(stat.ColumnId)).ToList();
                SortByPriority(statDeviationsColumns);
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

        private List<ComparedRow> IntersectRows(List<ComparedRow> comparedRows) {
            var masterGroup = comparedRows.GroupBy(item => item.MasterRowId).Where(item => item.Count() > 1).Select(item => item.Key);
            var testGroup = comparedRows.GroupBy(item => item.TestRowId).Where(item => item.Count() > 1).Select(item => item.Key);
            return comparedRows.Where(item => !masterGroup.Contains(item.MasterRowId) && !testGroup.Contains(item.TestRowId)).ToList();
        }

        private void SortByPriority(List<ColumnSummary> statDeviationsColumns) {
            var columnsOrderedByPriority = statDeviationsColumns.OrderByDescending(col => col.MatchingRate).ThenByDescending(col => col.UniqMatchRate);
            List<ComparedRow> bestMatched = new List<ComparedRow>();
            foreach (var item in columnsOrderedByPriority) {
                ComparisonTask.IfCancelRequested();
                foreach (var cr in BestCombinations) {
                    ComparisonTask.IfCancelRequested();
                    var deviation = cr.Deviations.Where(d => d.ColumnId == item.ColumnId).FirstOrDefault();
                    if(deviation != null) {
                        deviation.CalculateDiff(item.IsString);
                    }                
                }
                var min = BestCombinations.SelectMany(g => g.Deviations.Select(i => i.Difference)).Min();
                bestMatched = BestCombinations.Where(row => row.Deviations.Select(col => col.Difference).Contains(min)).ToList();
                var res = IntersectRows(bestMatched);
                foreach (var m in res) {
                    AddComparedRow(m);
                    bestMatched.RemoveAll(i=>i.MasterRowId == m.MasterRowId || i.TestRowId == m.TestRowId );
                }
                if (bestMatched.Any()) {
                    BestCombinations.Clear();
                    BestCombinations.AddRange(bestMatched);
                }else {
                    return;
                }
            }
            if (bestMatched.Any()) {
                var orderedBestMatched = bestMatched.OrderBy(i=>i.TestRowId);
                AddComparedRow(orderedBestMatched.First());
            }
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


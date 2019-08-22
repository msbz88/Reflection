using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ComparisonCore {
        RowsMatch RowsMatch;
        Comparator Comparator;
        List<string> ComparedRowSB = new List<string>();
        PerformanceCounter PerfCounter;
        CompareTable CompareTable;
        IWorkTable MasterTable;
        IWorkTable TestTable;
        ComparisonTask ComparisonTask { get; set; }
        string Delimiter;
        public List<ColumnSummary> BaseStat;

        public ComparisonCore(PerformanceCounter perfCounter, ComparisonTask comparisonTask) {
            PerfCounter = perfCounter;
            ComparisonTask = comparisonTask;
        }

        public CompareTable Execute(IWorkTable masterTable, IWorkTable testTable) {
            MasterTable = masterTable;
            TestTable = testTable;
            Delimiter = SetDelimiter();
            CompareTable = new CompareTable(MasterTable.Headers, TestTable.Headers, ComparisonTask);
            //gather base stat  
            PerfCounter.Start();
            BaseStat = GatherStatistics(MasterTable.Rows, TestTable.Rows);
            ComparisonTask.IfCancelRequested();
            PerfCounter.Stop("Base Gather Stat");
            //analyse
            //File.WriteAllLines(@"C:\Users\MSBZ\Desktop\baseStat.txt", BaseStat.Select(r => r.ToString()));
            PerfCounter.Start();
            var comparisonKeys = AnalyseForPivotKey(MasterTable.Rows, BaseStat, MasterTable.Headers.Data);
            if (ComparisonTask.ComparisonKeys.UserKeys.Count == 0) {
                ComparisonTask.ComparisonKeys.MainKeys = comparisonKeys.MainKeys;
            } else {
                ComparisonTask.ComparisonKeys.MainKeys = ComparisonTask.ComparisonKeys.UserKeys;
            }
            ComparisonTask.ComparisonKeys.BinaryValues = comparisonKeys.BinaryValues;
            ComparisonTask.ComparisonKeys.ExcludeColumns = comparisonKeys.ExcludeColumns;
            ComparisonTask.ComparisonKeys.UserIdColumns = comparisonKeys.UserIdColumns;
            ComparisonTask.ComparisonKeys.UserIdColumnsBinary = comparisonKeys.UserIdColumnsBinary;
            ComparisonTask.IsKeyReady = true;
            ComparisonTask.IfCancelRequested();
            PerfCounter.Stop("AnalyseForPivotKey");
            //File.AppendAllText(@"C:\Users\MSBZ\Desktop\baseStat.txt", "baseKeyIndex: " + string.Join(";", MasterTable.Headers.ColumnIndexIn(PivotKeysIndexes.MainKeys)));
            //rows match
            RowsMatch = new RowsMatch(BaseStat, ComparisonTask.ComparisonKeys, ComparisonTask);
            Comparator = new Comparator(ComparisonTask.ComparisonKeys, ComparisonTask.IsDeviationsOnly);
            //group
            PerfCounter.Start();
            var groupsM = Group(MasterTable.Rows, ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            var groupsT = Group(TestTable.Rows, ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.UpdateProgress(2);
            PerfCounter.Stop("Base Group");
            PerfCounter.Start();
            ComparisonTask.IfCancelRequested();
            var uMasterRows = groupsM.Where(r => r.Value.Count() == 1).ToDictionary(item => item.Key, item => item.Value.First());
            ComparisonTask.UpdateProgress(2);
            ComparisonTask.IfCancelRequested();
            var uTestRows = groupsT.Where(r => r.Value.Count() == 1).ToDictionary(item => item.Key, item => item.Value.First());
            ComparisonTask.UpdateProgress(2);
            var resU = GroupMatch(uMasterRows, uTestRows).ToList();
            CompareTable.AddComparedRows(resU);
            ComparisonTask.UpdateProgress(2);
            ComparisonTask.IfCancelRequested();
            var mRemainings = Group(GetRemainings(MasterTable.Rows, CompareTable.GetMasterComparedRowsId()), ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.UpdateProgress(2);
            ComparisonTask.IfCancelRequested();
            var tRemainings = Group(GetRemainings(TestTable.Rows, CompareTable.GetTestComparedRowsId()), ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.UpdateProgress(2);
            ComparisonTask.IfCancelRequested();
            PerfCounter.Stop("Preparison");
            PerfCounter.Start();
            var groups = from m in mRemainings
                         join t in tRemainings on m.Key equals t.Key
                         select new { Key = m.Key, ComparedRows = RowsMatch.ProcessGroup(m.Value, t.Value, mRemainings.Count) };

            foreach (var item in groups) {
                ComparisonTask.IfCancelRequested();
                CompareTable.AddComparedRows(item.ComparedRows);
            }
            PerfCounter.Stop("Process");
            //extra
            PerfCounter.Start();
            ComparisonTask.RowsWithDeviations = CompareTable.ComparedRowsCount;
            ComparisonTask.IfCancelRequested();

            var masterExtra = GetRemainings(MasterTable.Rows, CompareTable.GetMasterComparedRowsId());
            ComparisonTask.IfCancelRequested();
            var testExtra = GetRemainings(TestTable.Rows, CompareTable.GetTestComparedRowsId());
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.IfCancelRequested();
            CompareTable.AddMasterExtraRows(masterExtra);
            ComparisonTask.ExtraMasterCount = CompareTable.MasterExtraCount;
            ComparisonTask.IfCancelRequested();
            CompareTable.AddTestExtraRows(testExtra);
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.ExtraTestCount = CompareTable.TestExtraCount;
            PerfCounter.Stop("Extra");
            PerfCounter.Start();
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            PerfCounter.Stop("Save comparison");
            //PerfCounter.SaveAllResults();      
            return CompareTable;
        }

        //private void UserKeyCorrection(List<int> userKeys) {
        //    foreach (var excludedCol in ComparisonTask.ComparisonKeys.UserExcludeColumns) {
        //        for (int i = 0; i < userKeys.Count; i++) {
        //            if(userKeys[i] > excludedCol) {
        //                userKeys[i] -= 1;
        //            }
        //        }
        //    }
        //}

        private string SetDelimiter() {
            if (MasterTable.Delimiter.Contains("|")) {
                return ";";
            } else {
                return MasterTable.Delimiter;
            }
        }

        private void FillSummary(int mRowsCount, int tRowsCount, int compRowsCount, int extraMasterCount, int extraTestCount) {
            var actRowsDiff = mRowsCount >= tRowsCount ? mRowsCount - tRowsCount : tRowsCount - mRowsCount;
            ComparisonTask.MasterRowsCount = mRowsCount;
            ComparisonTask.TestRowsCount = tRowsCount;
            ComparisonTask.ActualRowsDiff = actRowsDiff;
            ComparisonTask.RowsWithDeviations = compRowsCount;
            ComparisonTask.ExtraMasterCount = extraMasterCount;
            ComparisonTask.ExtraMasterCount = extraTestCount;
        }

        private IEnumerable<Row> GetRemainings(List<Row> rows, IEnumerable<int> comparedRowsId) {
            var filter = rows.Select(row => row.Id).Except(comparedRowsId).ToList();
            return rows.Where(row => filter.Contains(row.Id));
        }

        private IEnumerable<ComparedRow> GroupMatch(Dictionary<string, Row> masterRows, Dictionary<string, Row> testRows) {
            var query = from m in masterRows
                        join t in testRows on m.Key equals t.Key
                        select Comparator.CompareSingle(m.Value, t.Value);
            return query.Where(item => item != null);
        }

        private Dictionary<string, List<Row>> Group(IEnumerable<Row> rows, List<int> pivotFields) {
            return rows.GroupBy(col => string.Join(" | ", col.ColumnIndexIn(pivotFields))).ToDictionary(group => group.Key, group => group.ToList());
        }

        private bool IsUsefulInCompositeKey(Dictionary<string, List<Row>> rows, List<int> pivotFields, int analysisColumn) {
            return rows.Any(grp => grp.Value.Select(row => row[analysisColumn]).Distinct().Count() > 1);
        }

        private int FindInsType(string[] headers) {
            Dictionary<int, string> columnNames = new Dictionary<int, string>();
            for (int i = 0; i < headers.Length; i++) {
                columnNames.Add(i, headers[i]);
            }
            var insType = columnNames.Where(item =>
            item.Value.ToLower().Contains("instype")
            || item.Value.ToLower().Contains("ins_type")
            || item.Value.ToLower() == "instrument_type"
            || item.Value.ToLower() == "instrument type")
            .FirstOrDefault();
            return insType.Value == null ? -1 : insType.Key;
        }

        public ComparisonKeys AnalyseForPivotKey(IEnumerable<Row> sampleRows, List<ColumnSummary> baseStat, string[] headers) {
            ComparisonKeys compKeys = new ComparisonKeys();
            var userExcludeColumns = ComparisonTask.MasterConfiguration.UserExcludeColumns.Concat(ComparisonTask.TestConfiguration.UserExcludeColumns).OrderBy(item => item).Distinct().ToList();
            compKeys.UserExcludeColumns = userExcludeColumns;
            var clearedStats = baseStat.Where(col => !col.IsDouble && !col.IsNumber && !col.HasNulls && !col.IsTransNo && !col.IsTimestamp && !userExcludeColumns.Contains(col.ColumnId)).ToList();
            if (!clearedStats.Any()) {
                clearedStats = baseStat;
            }
            var maxMatchingRate = clearedStats.Max(col => col.MatchingRate);
            var maxUniqMatchRate = clearedStats.Where(col => col.MatchingRate == maxMatchingRate).Max(col => col.UniqMatchRate);
            var mainPivotKey = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.UniqMatchRate == maxUniqMatchRate).First().ColumnId;
            var additionalKeys = FindAdditionalKeys(clearedStats, maxMatchingRate, mainPivotKey);
            List<int> compositeKey = new List<int>() { mainPivotKey };
            var insType = FindInsType(headers);
            if (insType != -1) {
                compositeKey.Add(insType);
            }
            var groups = Group(sampleRows, compositeKey);
            ComparisonTask.UpdateProgress(1);
            foreach (var key in additionalKeys) {
                if (IsUsefulInCompositeKey(groups, compositeKey, key)) {
                    compositeKey.Add(key);
                    groups = Group(groups.Where(grp => grp.Value.Count() > 1).SelectMany(row => row.Value), compositeKey);
                }
                ComparisonTask.UpdateProgress(10.0 / additionalKeys.Count);
            }
            if (compositeKey.Count == 1) {
                var clearedStatsWithNum = baseStat.Where(col => !col.IsDouble && !col.HasNulls && !col.IsTransNo && !col.IsTimestamp && !userExcludeColumns.Contains(col.ColumnId)).ToList();
                compositeKey.AddRange(AddKeysToAcceptExtra(clearedStatsWithNum, maxMatchingRate, mainPivotKey));
            }
            var excludeColumns = baseStat.Where(item => item.IsTimestamp).Select(item => item.ColumnId).ToList();
            compKeys.BinaryValues = baseStat.Where(item => item.IsTransNo).Select(item => item.ColumnId).Except(ComparisonTask.ComparisonKeys.UserKeys).Except(userExcludeColumns).ToList();
            compKeys.MainKeys = compositeKey;
            compKeys.ExcludeColumns = excludeColumns.Concat(userExcludeColumns).ToList();
            var userIdColumns = ComparisonTask.MasterConfiguration.UserIdColumns.Concat(ComparisonTask.TestConfiguration.UserIdColumns).Distinct().Except(compKeys.MainKeys).Except(compKeys.BinaryValues);
            foreach (var item in userIdColumns) {
                if (baseStat.Where(col => col.ColumnId == item).First().MatchingRate != 100) {
                    compKeys.UserIdColumnsBinary.Add(item);
                } else {
                    compKeys.UserIdColumns.Add(item);
                }
            }
            return compKeys;
        }

        private List<int> FindAdditionalKeys(List<ColumnSummary> clearedStats, double maxMatchingRate, int mainPivotKey) {
            var statForAdditionalKeys = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.ColumnId != mainPivotKey).ToList();
            var maxDistinctMatch = clearedStats.Max(item => item.UniqDistinctMatchRate);
            var notStringKeys = statForAdditionalKeys.Where(col => !col.IsString && col.UniqMatchCount > 2 && col.UniqDistinctMatchRate >= maxDistinctMatch).Select(col => col.ColumnId);
            var stringKeys = statForAdditionalKeys.Where(col => col.IsString && col.UniqDistinctMatchRate >= maxDistinctMatch && col.UniqMatchCount > 2).Select(col => col.ColumnId);
            return stringKeys.Concat(notStringKeys).Distinct().ToList();
        }

        private List<int> AddKeysToAcceptExtra(List<ColumnSummary> clearedStats, double maxMatchingRate, int mainPivotKey) {
            var statForAdditionalKeys = clearedStats.Where(col => col.MatchingRate <= maxMatchingRate && col.ColumnId != mainPivotKey).ToList();
            var standartDeviation = StandartDeviation(statForAdditionalKeys.Select(col => col.MatchingRate).ToList());
            var notStringKeys = statForAdditionalKeys.Where(col => !col.IsString && col.UniqMatchCount > 2 && col.MatchingRate >= standartDeviation).Select(col => col.ColumnId);
            var stringKeys = statForAdditionalKeys.Where(col => col.IsString && col.MatchingRate >= standartDeviation).Select(col => col.ColumnId);
            return stringKeys.Concat(notStringKeys).Distinct().ToList();
        }

        private double StandartDeviation(List<double> values) {
            if (values.Count == 0) {
                return 0;
            }
            var avg = values.Average();
            var variance = (1.0 / values.Count) * values.Select(item => Math.Pow(item - avg, 2)).Sum();
            return Math.Sqrt(variance);
        }

        private Dictionary<int, HashSet<string>> GetColumns(IEnumerable<Row> rows) {
            Dictionary<int, HashSet<string>> columns = new Dictionary<int, HashSet<string>>();
            var firstLine = rows.FirstOrDefault();
            var columnsCount = firstLine.Data.Length;
            for (int i = 0; i < columnsCount; i++) {
                HashSet<string> uniqVals = new HashSet<string>();
                columns.Add(i, uniqVals);
            }
            ComparisonTask.UpdateProgress(2);
            int index = 0;
            foreach (var row in rows) {
                foreach (var item in row.Data) {
                    if (index < columns.Count) {
                        columns[index++].Add(item);
                    }
                }
                index = 0;
            }
            ComparisonTask.UpdateProgress(2);
            return columns;
        }

        public List<ColumnSummary> GatherStatistics(List<Row> masterRows, List<Row> testRows) {
            var masterColumns = GetColumns(masterRows);
            var testColumns = GetColumns(testRows);
            ComparisonTask.UpdateProgress(2);
            var totalRowsCount = masterRows.Count > testRows.Count ? testRows.Count : masterRows.Count;
            if (totalRowsCount == 0) {
                return new List<ColumnSummary>();
            }
            var columnsSummary = masterColumns.Join(testColumns,
                m => m.Key,
                t => t.Key,
                (m, t) => new ColumnSummary(m.Key, totalRowsCount, m.Value, t.Value)).ToList();
            ComparisonTask.UpdateProgress(2);
            return columnsSummary.ToList();
        }

    }
}

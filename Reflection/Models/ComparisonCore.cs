using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ComparisonCore {
        RowsMatch RowsMatch;
        List<string> ComparedRowSB = new List<string>();
        PerformanceCounter PerfCounter;
        CompareTable CompareTable;
        IWorkTable MasterTable;
        IWorkTable TestTable;
        ComparisonTask ComparisonTask { get; set; }
        string Delimiter;
        public List<ColumnSummary> BaseStat;
        List<int> PivotKeysIndexes { get; set; }

        public ComparisonCore(PerformanceCounter perfCounter, ComparisonTask comparisonTask) {
            PerfCounter = perfCounter;
            ComparisonTask = comparisonTask;          
        }

        public CompareTable Execute(IWorkTable masterTable, IWorkTable testTable) {
            MasterTable = masterTable;
            TestTable = testTable;
            Delimiter = SetDelimiter();
            //gather base stat  
            PerfCounter.Start();
            BaseStat = GatherStatistics(masterTable.Rows, testTable.Rows);
            PerfCounter.Stop("Base Gather Stat");
            //analyse
            File.WriteAllLines(@"C:\Users\MSBZ\Desktop\baseStat.txt", BaseStat.Select(r => r.ToString()));
            PerfCounter.Start();
            PivotKeysIndexes = AnalyseForPivotKey(masterTable.Rows, BaseStat);
            PerfCounter.Stop("AnalyseForPivotKey");
            File.AppendAllText(@"C:\Users\MSBZ\Desktop\baseStat.txt", "baseKeyIndex: " + string.Join(";", masterTable.Headers.ColumnIndexIn(PivotKeysIndexes)));
            //rows match
            RowsMatch = new RowsMatch(BaseStat, PivotKeysIndexes);
            //group
            PerfCounter.Start();
            var groupsM = Group(masterTable.Rows, PivotKeysIndexes);
            var groupsT = Group(testTable.Rows, PivotKeysIndexes);
            PerfCounter.Stop("Base Group");
            File.WriteAllText(@"C:\Users\MSBZ\Desktop\groupsM.txt", "Hash" + ";" + string.Join(";", masterTable.Headers.ColumnIndexIn(PivotKeysIndexes)) + Environment.NewLine);
            File.AppendAllLines(@"C:\Users\MSBZ\Desktop\groupsM.txt", groupsM.SelectMany(item => item.Value.Select(it => it.GetValuesHashCode(PivotKeysIndexes) + ";" + string.Join(";", it.ColumnIndexIn(PivotKeysIndexes)))));
            File.WriteAllText(@"C:\Users\MSBZ\Desktop\groupsT.txt", "Hash" + ";" + string.Join(";", masterTable.Headers.ColumnIndexIn(PivotKeysIndexes)) + Environment.NewLine);
            File.AppendAllLines(@"C:\Users\MSBZ\Desktop\groupsT.txt", groupsT.SelectMany(item => item.Value.Select(it => it.GetValuesHashCode(PivotKeysIndexes) + ";" + string.Join(";", it.ColumnIndexIn(PivotKeysIndexes)))));
            PerfCounter.Start();
            CompareTable = new CompareTable(Delimiter, MasterTable.Headers, TestTable.Headers);
            var uMasterRows = groupsM.Where(r => r.Value.Count() == 1).ToDictionary(item => item.Key, item => item.Value.First());
            var uTestRows = groupsT.Where(r => r.Value.Count() == 1).ToDictionary(item => item.Key, item => item.Value.First());
            var resU = Match(uMasterRows, uTestRows).ToList();
            CompareTable.AddComparedRows(resU);
            PerfCounter.Stop("Preparison");

            PerfCounter.Start();
            var mRemainings = Group(GetRemainings(MasterTable.Rows, CompareTable.GetMasterComparedRowsId()), PivotKeysIndexes);
            var tRemainings = Group(GetRemainings(TestTable.Rows, CompareTable.GetTestComparedRowsId()), PivotKeysIndexes);

            var groups = from m in mRemainings
                         join t in tRemainings on m.Key equals t.Key
                         select new { Key = m.Key, ComparedRows = RowsMatch.ProcessGroup(m.Value, t.Value) };

            foreach (var item in groups) {
                CompareTable.AddComparedRows(item.ComparedRows);
            }
            PerfCounter.Stop("Process");

            //extra
            //PerfCounter.Start();
            ComparisonTask.ComparedRows = CompareTable.ComparedRowsCount;
            var masterExtra = GetRemainings(MasterTable.Rows, CompareTable.GetMasterComparedRowsId());
            var testExtra = GetRemainings(MasterTable.Rows, CompareTable.GetMasterComparedRowsId());
            CompareTable.AddMasterExtraRows(masterExtra);
            ComparisonTask.ExtraMasterCount = CompareTable.MasterExtraCount;
            CompareTable.AddTestExtraRows(testExtra);
            ComparisonTask.ExtraTestCount = CompareTable.TestExtraCount;
            PerfCounter.Stop("Extra");
            
            //save to file
            string comparedRecordsFile = @"C:\Users\MSBZ\Desktop\comparedRecords.txt";
            string extraRecordsFile = @"C:\Users\MSBZ\Desktop\extra.txt";

            PerfCounter.Start();
            CompareTable.SaveComparedRows(comparedRecordsFile);
            CompareTable.SaveExtraRows(extraRecordsFile);
            PerfCounter.Stop("Save comparison");
            PerfCounter.SaveAllResults();
            return CompareTable;
        }

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
            ComparisonTask.ComparedRows = compRowsCount;
            ComparisonTask.ExtraMasterCount = extraMasterCount;
            ComparisonTask.ExtraMasterCount = extraTestCount;
            ComparisonTask.Progress = 100;
        }

        //public void ApplyRowNumberInGroup(IEnumerable<Row> rows, List<int> compKeys) {
        //    var query = from r in rows
        //                group r by r.GetValuesHashCode(compKeys)
        //                into g
        //                where g.Count() > 1
        //                //orderby g.Select(r=>r.MaterialiseKey(orderBy))                       
        //                select g;
        //    foreach (var group in query) {
        //        int RowNumber = 0;
        //        foreach (var row in group) {
        //            row.GroupId = RowNumber++;
        //        }
        //    }
        //}

        private IEnumerable<Row> GetRemainings(List<Row> rows, IEnumerable<int> comparedRowsId) {
            var filter = rows.Select(row=>row.Id).Except(comparedRowsId).ToList();
            return rows.Where(row => filter.Contains(row.Id));
        }

        private IEnumerable<ComparedRow> Match(Dictionary<string, Row> masterRows, Dictionary<string, Row> testRows) {
            return from m in masterRows
                   join t in testRows on m.Key equals t.Key 
                   select RowsMatch.Compare(m.Value, t.Value);
        }

        private List<int> AnalyseInGroup(List<ColumnSummary> columnsStat) {
            var uniqKey = columnsStat.FirstOrDefault(col => col.UniqMatchRate == 100);
            var clearedStats = columnsStat.Where(col => col.MatchingRate != 0 && !col.IsDouble).ToList();
            if (uniqKey != null) {
                return new List<int>() { uniqKey.ColumnId };
            } else if (clearedStats.Count == 0) {
                return new List<int>();
            } else {
                var maxMatchingRate = clearedStats.Max(col => col.MatchingRate);
                var maxUniqMatchRate = clearedStats.Where(col => col.MatchingRate == maxMatchingRate).Max(col => col.UniqMatchRate);
                var compKeys = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.UniqMatchRate == maxUniqMatchRate).Select(col => col.ColumnId);
                return compKeys.ToList();
            }
        }

        private Dictionary<string, List<Row>> Group(IEnumerable<Row> rows, List<int> pivotFields) {
            return rows.GroupBy(col => string.Join(" | ", col.ColumnIndexIn(pivotFields))).ToDictionary(group => group.Key, group => group.ToList());
        }

        private bool IsUsefulInCompositeKey(Dictionary<string, List<Row>> rows, List<int> pivotFields, int analysisColumn) {
            return rows.Any(grp => grp.Value.Select(row => row[analysisColumn]).Distinct().Count() > 1);
        }

        private List<int> AnalyseForPivotKey(IEnumerable<Row> sampleRows, List<ColumnSummary> columnsStat) {
            var uniqKey = columnsStat.FirstOrDefault(col => col.UniqMatchRate == 100);
            if (uniqKey != null) {
                return new List<int>() { uniqKey.ColumnId };
            }
            var clearedStats = columnsStat.Where(col => !col.IsDouble && !col.HasNulls);
            var maxMatchingRate = clearedStats.Max(col => col.MatchingRate);
            var maxUniqMatchRate = clearedStats.Where(col => col.MatchingRate == maxMatchingRate).Max(col => col.UniqMatchRate);
            var mainPivotKey = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.UniqMatchRate == maxUniqMatchRate).First().ColumnId;
            var additionalKeys = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.ColumnId != mainPivotKey).Select(col => col.ColumnId);
            List<int> compositeKey = new List<int>() { mainPivotKey };
            var groups = Group(sampleRows, compositeKey);
            foreach (var key in additionalKeys) {
                if (IsUsefulInCompositeKey(groups, compositeKey, key)) {
                    compositeKey.Add(key);
                    groups = Group(groups.Where(grp => grp.Value.Count() > 1).SelectMany(row => row.Value), compositeKey);
                }
            }
            return compositeKey;
        }

        //public async Task<Dictionary<int, HashSet<string>>> GetColumnsAsync(IEnumerable<Row> rows) {
        //    return await Task.Run(() => GetColumns(rows));
        //}

        private Dictionary<int, HashSet<string>> GetColumns(IEnumerable<Row> rows) {
            Dictionary<int, HashSet<string>> columns = new Dictionary<int, HashSet<string>>();
            var firstLine = rows.FirstOrDefault();
            var columnsCount = firstLine.Data.Length;
            for (int i = 0; i < columnsCount; i++) {
                HashSet<string> uniqVals = new HashSet<string>();
                columns.Add(i, uniqVals);
            }
            int index = 0;
            foreach (var row in rows) {
                foreach (var item in row.Data) {
                    columns[index++].Add(item);
                }
                index = 0;
            }
            return columns;
        }

        public List<ColumnSummary> GatherStatistics(IEnumerable<Row> masterRows, IEnumerable<Row> testRows) {
            var masterColumns = GetColumns(masterRows);
            var testColumns = GetColumns(testRows);
            var totalRowsCount = masterRows.Count() > testRows.Count() ? testRows.Count() : masterRows.Count();
            if (totalRowsCount == 0) {
                return new List<ColumnSummary>();
            }
            var columnsSummary = masterColumns.Join(testColumns,
                m => m.Key,
                t => t.Key,
                (m, t) => new ColumnSummary(m.Key, totalRowsCount, m.Value, t.Value)).ToList();
            return columnsSummary.ToList();
        }
    }
}

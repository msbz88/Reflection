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
        CompareTable CompareTable;
        IWorkTable MasterTable;
        IWorkTable TestTable;
        ComparisonTask ComparisonTask { get; set; }
        char[] Delimiter;
        public List<ColumnSummary> BaseStat;

        public ComparisonCore(ComparisonTask comparisonTask) {
            ComparisonTask = comparisonTask;
        }

        public CompareTable Execute(IWorkTable masterTable, IWorkTable testTable, UserKeys userKeys) {
            MasterTable = masterTable;
            TestTable = testTable;
            Delimiter = SetDelimiter();
            CompareTable = new CompareTable(MasterTable.Headers, TestTable.Headers, ComparisonTask);
            //gather base stat  
            BaseStat = GatherStatistics(MasterTable.Rows, TestTable.Rows);
            ComparisonTask.IfCancelRequested();
            //analyse
            var sampleRows = MasterTable.RowsCount > TestTable.RowsCount ? MasterTable.Rows : TestTable.Rows;
            var numberedHeaders = Helpers.NumerateSequence(masterTable.Headers.Data);
            ComparisonTask.ComparisonKeys = MergeComparisonKeys(userKeys, sampleRows, numberedHeaders, BaseStat);
            ComparisonTask.IfCancelRequested();
            //rows match
            RowsMatch = new RowsMatch(BaseStat, ComparisonTask.ComparisonKeys, ComparisonTask);
            Comparator = new Comparator(ComparisonTask.ComparisonKeys, ComparisonTask.IsDeviationsOnly);
            //group
            var groupsM = Group(MasterTable.Rows, ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            var groupsT = Group(TestTable.Rows, ComparisonTask.ComparisonKeys.MainKeys);
            ComparisonTask.UpdateProgress(2);
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
            var groups = from m in mRemainings
                         join t in tRemainings on m.Key equals t.Key
                         select new { Key = m.Key, ComparedRows = RowsMatch.ProcessGroup(m.Value, t.Value, mRemainings.Count) };
            foreach (var item in groups) {
                ComparisonTask.IfCancelRequested();
                CompareTable.AddComparedRows(item.ComparedRows);
            }
            //extra
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
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            return CompareTable;
        }

        private char[] SetDelimiter() {
            if (MasterTable.Delimiter.Any(item => item == '|')) {
                return new char[] { ';' };
            } else {
                return MasterTable.Delimiter;
            }
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

        private Dictionary<string, List<Row>> Group(IEnumerable<Row> rows, HashSet<int> pivotFields) {
            return rows.GroupBy(col => string.Join(" | ", Helpers.GetValuesByPositions(col.Data, pivotFields))).ToDictionary(group => group.Key, group => group.ToList());
        }

        private bool IsUsefulInCompositeKey(Dictionary<string, List<Row>> rows, HashSet<int> pivotFields, int analysisColumn) {
            return rows.Any(grp => grp.Value.Select(row => row[analysisColumn]).Distinct().Count() > 1);
        }

        public ComparisonKeys MergeComparisonKeys(UserKeys userKeys, List<Row> sampleRows, Dictionary<int, string> numberedHeaders, List<ColumnSummary> baseStat) {
            var comparisonKeys = new ComparisonKeys();
            //prepare comparison key
            HashSet<int> mainKeys;
            if (userKeys.UserComparisonKeys.Count == 0) {
                mainKeys = AnalyseForComparisonKeys(sampleRows, baseStat, userKeys.UserExcludeColumns, numberedHeaders);
            } else {
                mainKeys = userKeys.UserComparisonKeys;
            }
            comparisonKeys.MainKeys = mainKeys;
            //prepare id columns
            HashSet<int> singleIdColumns = new HashSet<int>();
            HashSet<int> binaryIdColumns = new HashSet<int>(AnalyseForBinaryIdColumns(baseStat));
            foreach (var columnId in userKeys.UserIdColumns) {
                var itemFound = baseStat.Find(item => item.ColumnId == columnId);
                if (itemFound != null && itemFound.MatchingRate == 100) {
                    singleIdColumns.Add(columnId);
                } else {
                    binaryIdColumns.Add(columnId);
                }
            }
            comparisonKeys.SingleIdColumns = new HashSet<int>(singleIdColumns.Except(mainKeys));
            comparisonKeys.BinaryIdColumns = new HashSet<int>(binaryIdColumns);
            //prepare exclude columns
            comparisonKeys.ExcludeColumns = new HashSet<int>(AnalyseForExcludeColumns(numberedHeaders, baseStat).Concat(userKeys.UserExcludeColumns).Except(mainKeys).Distinct());
            ComparisonTask.IsKeyReady = true;
            return comparisonKeys;
        }

        private int FindInsType(Dictionary<int, string> numberedHeaders) {
            var insType = numberedHeaders
                .Where(item => item.Value != null)
                .Where(item =>            
                    item.Value.ToLower().Contains("instype")
                    || item.Value.ToLower().Contains("ins_type")
                    || item.Value.ToLower() == "instrument_type"
                    || item.Value.ToLower() == "instrument type")
                    .FirstOrDefault();
            return insType.Value == null ? -1 : insType.Key;
        }

        private List<int> DetectUserIks(Dictionary<int, string> numberedHeaders, List<ColumnSummary> stat) {
            var columnNamesIK = numberedHeaders
                .Where(item => item.Value != null)
                .Where(item => item.Value.Contains("USR") && (item.Value.Contains("CRE") || item.Value.Contains("CHG")))
                .Select(item => item.Key).ToList();
            return (from st in stat
                    join ik in columnNamesIK on st.ColumnId equals ik
                    where st.IsNumber
                    select ik).ToList();
        }

        private int SearchForSecId(Dictionary<int, string> numberedHeaders) {
            var secId = numberedHeaders.Where(item => item.Value != null)
                .Where(item =>
                item.Value.ToLower().Contains("secshort")
                || item.Value.ToLower().Contains("secid")
                || item.Value.ToLower().Contains("sec_id")
                || item.Value.ToLower().Contains("sec id")
                || item.Value.ToLower().Contains("security id")
                || item.Value.ToLower().Contains("securityid")
                || item.Value.ToLower().Contains("security_id")
                ).FirstOrDefault();
            return secId.Value == null ? -1 : secId.Key;          
        }

        public HashSet<int> AnalyseForComparisonKeys(IEnumerable<Row> sampleRows, List<ColumnSummary> baseStat, HashSet<int> excludeColumns, Dictionary<int, string> numberedHeaders) {
            var clearedStats = baseStat.Where(col => !col.IsDouble && !col.IsNumber && !col.HasNulls && !col.IsTransNo && !col.IsTimestamp && !excludeColumns.Contains(col.ColumnId)).ToList();
            if (!clearedStats.Any()) {
                clearedStats = baseStat;
            }
            var maxMatchingRate = clearedStats.Max(col => col.MatchingRate);
            var maxUniqMatchRate = clearedStats.Where(col => col.MatchingRate == maxMatchingRate).Max(col => col.UniqMatchRate);
            var mainKey = clearedStats.Where(col => col.MatchingRate == maxMatchingRate && col.UniqMatchRate == maxUniqMatchRate).First().ColumnId;
            var additionalKeys = FindAdditionalKeys(clearedStats, maxMatchingRate, mainKey);
            HashSet<int> comparisonKeys = new HashSet<int>() { mainKey };
            var keysByHeaders = SearchKeysByHeadersNames(numberedHeaders, baseStat);
            ExtendComparisonKey(comparisonKeys, keysByHeaders);
            var usefulAdditionalKeys = GetUsefulComparisonKeys(sampleRows, comparisonKeys, additionalKeys);
            ExtendComparisonKey(comparisonKeys, usefulAdditionalKeys);
            if (comparisonKeys.Count == 1) {
                var clearedStatsWithNum = baseStat.Where(col => !col.IsDouble && !col.IsTransNo && !col.IsTimestamp && !excludeColumns.Contains(col.ColumnId)).ToList();
                var keysWithExtra = AddKeysToAcceptExtra(clearedStatsWithNum, maxMatchingRate, mainKey);
                var usefulKeysWithExtra = GetUsefulComparisonKeys(sampleRows, comparisonKeys, keysWithExtra);
                ExtendComparisonKey(comparisonKeys, usefulKeysWithExtra);
            }
            return comparisonKeys;
        }

        private void ExtendComparisonKey(HashSet<int> comparisonKeys, List<int> keysToAdd) {
            foreach (var key in keysToAdd) {
                comparisonKeys.Add(key);
            }
        }

        private List<int> GetUsefulComparisonKeys(IEnumerable<Row> sampleRows, HashSet<int> comparisonKeys, List<int> keysToCheck) {
            var groups = Group(sampleRows, comparisonKeys);
            var usefulKeys = new HashSet<int>(comparisonKeys);
            foreach (var key in keysToCheck) {
                if (IsUsefulInCompositeKey(groups, usefulKeys, key)) {
                    usefulKeys.Add(key);
                    groups = Group(groups.Where(grp => grp.Value.Count() > 1).SelectMany(row => row.Value), comparisonKeys);
                }
                ComparisonTask.UpdateProgress(5.0 / keysToCheck.Count);
            }
            return usefulKeys.Except(comparisonKeys).ToList();
        }

        public List<int> AnalyseForExcludeColumns(Dictionary<int, string> numberedHeaders, List<ColumnSummary> baseStat) {
            var detectedIks = DetectIKs(numberedHeaders, baseStat);
            var detectedUserIks = DetectUserIks(numberedHeaders, baseStat);
            var detectedTimestamps = baseStat.Where(item => item.IsTimestamp).Select(item => item.ColumnId);
            var detectedTransNumbers = baseStat.Where(item => item.IsTransNo).Select(item => item.ColumnId);
            return detectedTransNumbers.Concat(detectedIks).Concat(detectedUserIks).Concat(detectedTimestamps).Distinct().ToList();
        }

        public List<int> SearchKeysByHeadersNames(Dictionary<int, string> numberedHeaders, List<ColumnSummary> baseStat) {
            var idColumns = new List<int>();
            var insType = FindInsType(numberedHeaders);
            var secId = SearchForSecId(numberedHeaders);
            if (insType != -1 && baseStat.Where(item => item.ColumnId == insType).FirstOrDefault().UniqMatchCount > 0) {
                idColumns.Add(insType);
            }
            if (secId != -1 && baseStat.Where(item => item.ColumnId == secId).FirstOrDefault().UniqMatchCount > 0) {
                idColumns.Add(secId);
            }
            return idColumns;           
        }

        public List<int> AnalyseForBinaryIdColumns(List<ColumnSummary> baseStat) {
            var detectedTransNumbers = baseStat.Where(item => item.IsTransNo).Select(item => item.ColumnId);
            return detectedTransNumbers.ToList();
        }

        private List<int> DetectIKs(Dictionary<int, string> numberedHeaders,  List<ColumnSummary> stat) {
            var columnNamesIK = numberedHeaders.Where(item => item.Value != null).Where(item => item.Value.EndsWith("IK") || item.Value.EndsWith("(IK)")).Select(item => item.Key).ToList();
            return (from st in stat
                    join ik in columnNamesIK on st.ColumnId equals ik
                    where st.IsNumber
                    select ik).ToList();
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
            if (statForAdditionalKeys.Any()) {
                var standartDeviation = statForAdditionalKeys.Select(col => col.MatchingRate).Average();
                var notStringKeys = statForAdditionalKeys.Where(col => !col.IsString && col.UniqMatchCount > 2 && col.MatchingRate >= standartDeviation).Select(col => col.ColumnId);
                var stringKeys = statForAdditionalKeys.Where(col => col.IsString && col.MatchingRate >= standartDeviation).Select(col => col.ColumnId);
                return stringKeys.Concat(notStringKeys).Distinct().ToList();
            }else {
                return new List<int>();
            }
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

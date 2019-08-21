using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class RowToSave {
        private string DefectNo { get; set; }
        private string Diff { get; set; }
        ComparedRow ComparedRow { get; set; }
        string[] MasterParsedExtraRow { get; set; }
        string[] TestParsedExtraRow { get; set; }

        public RowToSave(ComparedRow comparedRow) {
            ComparedRow = comparedRow;
            Diff = ComparedRow.Deviations.Count.ToString();
        }

        public RowToSave(string[] parsedExtraRow) {
            MasterParsedExtraRow = parsedExtraRow;
            Diff = MasterParsedExtraRow.Length.ToString();
        }

        public RowToSave(string[] masterParsedExtraRow, string[] testParsedExtraRow) {
            MasterParsedExtraRow = masterParsedExtraRow;
            TestParsedExtraRow = testParsedExtraRow;
            Diff = "0";
        }

        private void FindDefect(Dictionary<int, string> columnNames, DefectsSearch defectsSearch, Deviation deviation) {
            foreach (var transNo in ComparedRow.TransNoColumns) {
                var defect = defectsSearch.SearchDefectByTransNo(transNo.MasterValue, transNo.TestValue, columnNames[deviation.ColumnId]);
                defect = defect == "" ? "" : "TransMatch: " + defect;
                AddDefect(defect);
            }
            if (string.IsNullOrEmpty(DefectNo)) {
                var secCols = SearchForSecId(columnNames);
                var exSecCols = secCols.Intersect(ComparedRow.IdColumns.Select(item => item.Key));
                foreach (var col in exSecCols) {
                    var defect = defectsSearch.SearchDefectBySecId(ComparedRow.IdColumns[col], columnNames[deviation.ColumnId]);
                    defect = defect == "" ? "" : "SecMatch: " + defect + "?";
                    AddDefect(defect);
                }
            }
            if (string.IsNullOrEmpty(DefectNo)) {
                var defect = defectsSearch.SearchDefectByValue(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                defect = defect == "" ? "" : "ValMatch: " + defect + "?";
                AddDefect(defect);
            }
            if (string.IsNullOrEmpty(DefectNo)) {
                var defect = defectsSearch.SearchDefectByValueForSameUpgrades(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                defect = defect == "" ? "" : "UpgradeMatch: " + defect + "?";
                AddDefect(defect);
            }
            if (string.IsNullOrEmpty(DefectNo)) {
                var defect = defectsSearch.SearchDefectByValueInAllProjects(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                defect = defect == "" ? "" : "DeepMatch: " + defect + "?";
                AddDefect(defect);
            }
        }

        public List<List<string>> PrepareRowLinear(Dictionary<int, string> columnNames, DefectsSearch defectsSearch) {
            var result = new List<List<string>>();
            foreach (var deviation in ComparedRow.Deviations) {
                var innerResult = new List<string>();
                FindDefect(columnNames, defectsSearch, deviation);
                innerResult.Add(DefectNo);
                innerResult.Add("Deviations");
                innerResult.Add(Diff);
                foreach (var transNo in ComparedRow.TransNoColumns) {
                    innerResult.Add(transNo.MasterValue);
                    innerResult.Add(transNo.TestValue);
                }
                foreach (var idCol in ComparedRow.IdColumns) {
                    innerResult.Add(idCol.Value);
                }
                innerResult.Add(columnNames[deviation.ColumnId]);
                innerResult.Add(deviation.MasterValue);
                innerResult.Add(deviation.TestValue);
                result.Add(innerResult);
            }
            return result;
        }

        public List<string> PrepareRowTabular(Dictionary<int, string> columnNames, DefectsSearch defectsSearch, List<int> deviationsPattern) {
            var result = new List<string>();
            result.Add("");
            result.Add("Deviations");
            result.Add(Diff);
            foreach (var transNo in ComparedRow.TransNoColumns) {
                result.Add(transNo.MasterValue);
                result.Add(transNo.TestValue);
            }
            foreach (var idCol in ComparedRow.IdColumns) {
                result.Add(idCol.Value);
            }
            foreach (var columnId in deviationsPattern) {
                var deviation = ComparedRow.Deviations.Where(item => item.ColumnId == columnId).FirstOrDefault();
                if (deviation != null) {
                    result.Add(deviation.MasterValue + " | " + deviation.TestValue);
                } else {
                    result.Add("0");
                }
            }
            return result;
        }

        private List<int> SearchForSecId(Dictionary<int, string> columnNames) {
            return columnNames.Where(item =>
            item.Value.ToLower().Contains("secshort")
            || item.Value.ToLower().Contains("secid")
            || item.Value.ToLower().Contains("sec_id")
            ).Select(item => item.Key).ToList();
        }

        private void AddDefect(string defect) {
            if (!string.IsNullOrEmpty(defect)) {
                if (!string.IsNullOrEmpty(DefectNo)) {
                    DefectNo = DefectNo + ", " + defect;
                } else {
                    DefectNo = defect;
                }
            }
        }

        public List<string> PrepareExtraRow(string version, List<int> transNoColumns, List<int> mainIdColumns) {
            var result = new List<string>();
            result.Add("");
            result.Add("Extra from " + version);
            result.Add(Diff);
            List<string> transNoValues = new List<string>();
            foreach (var colId in transNoColumns) {
                if (version == "Master") {
                    transNoValues.Add(MasterParsedExtraRow[colId]);
                    transNoValues.Add("");
                } else {
                    transNoValues.Add("");
                    transNoValues.Add(MasterParsedExtraRow[colId]);
                }
            }
            var idColumnsValues = GetValuesByPositions(mainIdColumns);
            result.AddRange(transNoValues);
            result.AddRange(idColumnsValues);
            return result;
        }

        public List<string> PrepareExceptedRow(List<int> transNoColumns, List<int> mainIdColumns) {
            var result = new List<string>();
            result.Add("");
            result.Add("Passed");
            result.Add(Diff);
            List<string> transNoValues = new List<string>();
            foreach (var colId in transNoColumns) {
                transNoValues.Add(MasterParsedExtraRow[colId]);
                transNoValues.Add(TestParsedExtraRow[colId]);
            }
            var idColumnsValues = GetValuesByPositions(mainIdColumns);
            result.AddRange(transNoValues);
            result.AddRange(idColumnsValues);
            return result;
        }

        public List<string> PreparePassedRow() {
            var result = new List<string>();
            result.Add("");
            result.Add("Passed");
            result.Add("0");
            foreach (var transNo in ComparedRow.TransNoColumns) {
                result.Add(transNo.MasterValue);
                result.Add(transNo.TestValue);
            }
            foreach (var idCol in ComparedRow.IdColumns) {
                result.Add(idCol.Value);
            }
            return result;
        }

        private List<string> GetValuesByPositions(IEnumerable<int> positions) {
            var query = new List<string>();
            foreach (var item in positions) {
                query.Add(MasterParsedExtraRow[item]);
            }
            return query;
        }

    }
}

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
        string[] ParsedExtraRow { get; set; }

        public RowToSave(ComparedRow comparedRow) {
            ComparedRow = comparedRow;
            Diff = ComparedRow.Deviations.Count.ToString();
        }

        public RowToSave(string[] parsedExtraRow) {
            ParsedExtraRow = parsedExtraRow;
            Diff = ParsedExtraRow.Length.ToString();
        }

        public List<List<string>> PrepareRowLinear(Dictionary<int, string> columnNames, DefectsSearch defectsSearch) {
            var result = new List<List<string>>();
            foreach (var deviation in ComparedRow.Deviations) {
                var innerResult = new List<string>();
                if (defectsSearch.IsEnabled) {
                    try {
                        DefectNo = defectsSearch.FindDefect(columnNames, ComparedRow.TransNoColumns, ComparedRow.IdColumns, deviation);
                    } catch (Exception) {
                        defectsSearch.Disable();
                    }
                }
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

        public List<string> PrepareRowTabular(Dictionary<int, string> columnNames, List<int> deviationsPattern) {
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

        public List<string> PrepareExtraRow(string version, List<int> transNoColumns, List<int> mainIdColumns, DefectsSearch defectsSearch) {
            var result = new List<string>();
            //if (defectsSearch.IsEnabled) {
            //    defectsSearch.FindDefect(columnNames, deviation);
            //}
            result.Add(DefectNo);
            result.Add("Extra from " + version);
            result.Add(Diff);
            List<string> transNoValues = new List<string>();
            foreach (var colId in transNoColumns) {
                if (version == "Master") {
                    transNoValues.Add(ParsedExtraRow[colId]);
                    transNoValues.Add("");
                } else {
                    transNoValues.Add("");
                    transNoValues.Add(ParsedExtraRow[colId]);
                }
            }
            var idColumnsValues = Helpers.GetValuesByPositions(ParsedExtraRow, mainIdColumns);
            result.AddRange(transNoValues);
            result.AddRange(idColumnsValues);
            return result;
        }

        public List<string> PrepareExceptedRow(List<int> transNoColumns, List<int> mainIdColumns) {
            var result = new List<string>();
            result.Add("");
            result.Add("Passed");
            result.Add("0");
            List<string> transNoValues = new List<string>();
            foreach (var colId in transNoColumns) {
                transNoValues.Add(ParsedExtraRow[colId]);
                transNoValues.Add(ParsedExtraRow[colId]);
            }
            var idColumnsValues = Helpers.GetValuesByPositions(ParsedExtraRow, mainIdColumns);
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
    

    }
}

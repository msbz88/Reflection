using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class CompareTable {
        Row MasterHeaders { get; set; }
        Row TestHeaders { get; set; }
        List<ComparedRow> Data { get; set; }
        List<string> ExtraMaster { get; set; }
        List<string> ExtraTest { get; set; }
        string Delimiter { get; set; }

        public CompareTable(string delimiter, Row masterHeaders, Row testHeaders) {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            Delimiter = delimiter;
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
        }

        public void AddComparedRows(IEnumerable<ComparedRow> comparedRows) {
            Data.AddRange(comparedRows);
        }

        public void AddComparedRow(ComparedRow comparedRow) {
            Data.Add(comparedRow);
        }

        public void AddMasterExtraRows(IEnumerable<string> extraRows) {
            ExtraMaster.AddRange(extraRows);
        }

        public IEnumerable<int> GetMasterComparedRowsId() {
            return Data.Select(row=>row.MasterRowId);
        }

        public IEnumerable<int> GetTestComparedRowsId() {
            return Data.Select(row => row.TestRowId);
        }

        public void AddTestExtraRows(IEnumerable<string> extraRows) {
            ExtraTest.AddRange(extraRows);
        }

        private string GenerateHeadersForFile(List<int> columns) {
            var headersMaster = MasterHeaders.ColumnIndexIn(columns);
            var headersTest = TestHeaders.ColumnIndexIn(columns);
            StringBuilder headers = new StringBuilder();
            for (int i = 0; i < headersMaster.Count; i++) {
                if (headersMaster[i] != headersTest[i]) {
                    headers.Append(headersMaster[i] + " | " + headersTest[i]);
                } else {
                    headers.Append(headersMaster[i]);
                }
                if (i < columns.Count) {
                    headers.Append(Delimiter);
                }
            }
           return headers.ToString();
        }

        public void SaveComparedRows(string filePath) {
            var idFields = Data.SelectMany(row => row.IdFields.Select(col => col.Key)).Distinct().OrderBy(colId => colId).ToList();
            var allDeviationsColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList();
            Dictionary<int, string> rowToSave = new Dictionary<int, string>(allDeviationsColumns.Count);
            var allColumns = idFields.Concat(allDeviationsColumns).ToList();
            var headers = GenerateHeadersForFile(idFields.Concat(allDeviationsColumns).ToList());
            File.WriteAllText(filePath, headers);
            StringBuilder lines = new StringBuilder();
            StringBuilder cell = new StringBuilder();          
            foreach (var comparedRow in Data) {
                RefreshRowToSave(rowToSave, allColumns);
                foreach (var deviation in comparedRow.Deviations) {
                    cell.Clear();
                    cell.Append(deviation.MasterValue);
                    cell.Append(" | ");
                    cell.Append(deviation.TestValue);
                    if(comparedRow.Deviations.IndexOf(deviation) < allDeviationsColumns.Count) {
                        cell.Append(Delimiter);
                    }                   
                    rowToSave[deviation.ColumnId] = cell.ToString();
                }
                lines.Append(Environment.NewLine);
                AddIdentificationColumns(comparedRow.IdFields, rowToSave);
                foreach (var item in rowToSave) {
                    lines.Append(item.Value);
                }               
            }
            File.AppendAllText(filePath, lines.ToString());
        }

        private void RefreshRowToSave(Dictionary<int, string> rowToSave, List<int> allColumns) {
            rowToSave.Clear();
            foreach (var colId in allColumns) {
                rowToSave.Add(colId, "0");
            }
        }

        public void SaveExtraRows(string filePath) {
            StringBuilder line = new StringBuilder();
            var headers = CreateLine("Version", line, MasterHeaders.Data.ToList());
            File.WriteAllText(filePath, headers);
            foreach (var item in ExtraMaster) {
                line.Append("Master");
                line.Append(item);
                File.AppendAllText(filePath, item);
            }
            foreach (var item in ExtraTest) {
                line.Append("Test");
                line.Append(item);
                File.AppendAllText(filePath, item);
            }
        }

        private string CreateLine(string id, StringBuilder line, List<string> values) {
            line.Clear();
            line.Append(id);
            line.Append(Delimiter);
            foreach (var item in values) {
                line.Append(item);
                if (values.IndexOf(item) + 1 < values.Count) {
                    line.Append(Delimiter);
                }                
            }
            return line.ToString();
        }

        private void AddIdentificationColumns(Dictionary<int, string> idFields, Dictionary<int, string> rowToSave) {
            foreach (var item in idFields) {
                rowToSave[item.Key] = item.Value + Delimiter;
            }
        }


    }
}

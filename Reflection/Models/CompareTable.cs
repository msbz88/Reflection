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
        public int ComparedRowsCount { get { return Data.Count; } }
        List<string> ExtraMaster { get; set; }
        public int MasterExtraCount { get { return ExtraMaster.Count; } }
        List<string> ExtraTest { get; set; }
        public int TestExtraCount { get { return ExtraTest.Count; } }
        string Delimiter { get; set; }
        int TotalColumns { get; set; }
        ComparisonKeys ComparisonKeys { get; set; }

        public CompareTable(string delimiter, Row masterHeaders, Row testHeaders, int totalColumns, ComparisonKeys comparisonKeys) {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            Delimiter = delimiter;
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
            TotalColumns = totalColumns;
            ComparisonKeys = comparisonKeys;
        } 

        public void AddComparedRows(IEnumerable<ComparedRow> comparedRows) {
            Data.AddRange(comparedRows);
        }

        public void AddComparedRow(ComparedRow comparedRow) {
            Data.Add(comparedRow);
        }

        public void AddMasterExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraMaster.Add(string.Join(Delimiter, item.Data));
            }
        }

        public IEnumerable<int> GetMasterComparedRowsId() {
            return Data.Select(row=>row.MasterRowId);
        }

        public IEnumerable<int> GetTestComparedRowsId() {
            return Data.Select(row => row.TestRowId);
        }

        public void AddTestExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraTest.Add(string.Join(Delimiter, item.Data));
            }
        }

        private string GenerateHeadersForFile(List<int> transNo, List<int> columns, List<int> remaining) {
            StringBuilder headers = new StringBuilder();
            headers.Append("Version");
            headers.Append(Delimiter);
            foreach (var item in transNo) {
                headers.Append("M_" + MasterHeaders[item]);
                headers.Append(Delimiter);
                headers.Append("T_" + MasterHeaders[item]);
                headers.Append(Delimiter);
            }
            foreach (var item in columns) {
                headers.Append(MasterHeaders[item]);
                if (item != columns.Last()) {
                    headers.Append(Delimiter);
                }
            }
            if (MasterExtraCount > 0 || TestExtraCount > 0) {
                headers.Append(Delimiter);
                foreach (var item in remaining) {
                    headers.Append(MasterHeaders[item]);
                    if (item != columns.Last()) {
                        headers.Append(Delimiter);
                    }
                }
            }      
           return headers.ToString();
        }

        public void SaveComparedRows(string filePath) {
            var transNo = ComparisonKeys.AdditionalKeys;     
            var mainKeys = ComparisonKeys.MainKeys;
            var deviationsColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList(); 
            var allColumns = mainKeys.Concat(deviationsColumns).ToList();
            var remaining = Enumerable.Range(0, TotalColumns-1).Select(x => x).Except(transNo.Concat(allColumns)).ToList();
            var headers = GenerateHeadersForFile(transNo, allColumns, remaining);
            File.WriteAllText(filePath, headers);
            StringBuilder lines = new StringBuilder();
            StringBuilder cell = new StringBuilder();       
            foreach (var comparedRow in Data) {
                var rowToSave = CreateRowToSave(transNo, allColumns);
                foreach (var deviation in comparedRow.Deviations) {
                    cell.Clear();
                    cell.Append(deviation.MasterValue);
                    cell.Append(" | ");
                    cell.Append(deviation.TestValue);
                    cell.Append(Delimiter);             
                    rowToSave[deviation.ColumnId] = cell.ToString();
                }
                lines.Append(Environment.NewLine);
                AddIdentificationColumns(comparedRow.IdFields, rowToSave);
                var lastValue = rowToSave.Last();
                rowToSave[lastValue.Key] = lastValue.Value.TrimEnd(Delimiter.ToCharArray());
                foreach (var item in rowToSave) {
                    lines.Append(item.Value);
                }
            }
            File.AppendAllText(filePath, lines.ToString());
            SaveExtraRows(filePath, transNo, allColumns, remaining);
        }

        private Dictionary<int, string> CreateRowToSave(List<int> transNo, List<int> allColumns) {
            Dictionary<int, string> rowToSave = new Dictionary<int, string>(allColumns.Count + transNo.Count + 1);
            var version = "Master | Test" + Delimiter;
            rowToSave.Add(99999,version);
            foreach (var colId in transNo) {
                rowToSave.Add(colId * -1, "0" + Delimiter);
            }
            foreach (var colId in allColumns.Except(transNo)) {
                rowToSave.Add(colId, "0" + Delimiter);
            }
            return rowToSave;
        }

        public void SaveExtraRows(string filePath, List<int> transNo, List<int> allColumns, List<int> remaining) {
            if(MasterExtraCount == 0 && TestExtraCount == 0) {
                return;
            }
            //StringBuilder headers = new StringBuilder();           
            //headers.Append("Version");
            //foreach (var item in MasterHeaders.Data) {
            //    headers.Append(Delimiter);
            //    headers.Append(item);               
            //}
            //headers.Append(Environment.NewLine);
            //File.WriteAllText(filePath, headers.ToString());    
            File.AppendAllText(filePath, Environment.NewLine);
            File.AppendAllText(filePath, CreateLines("Master", ExtraMaster, transNo, allColumns, remaining));
            File.AppendAllText(filePath, CreateLines("Test", ExtraTest, transNo, allColumns, remaining));
        }

        private string CreateLines(string version, List<string> extraLines, List<int> transNo, List<int> allColumns, List<int> remaining) {
            StringBuilder lines = new StringBuilder();
            foreach (var item in extraLines) {
                var row = item.Split(new string[] { Delimiter }, StringSplitOptions.None);
                List<string> transNoPart = new List<string>();
                foreach (var i in transNo) {
                    if (version == "Master") {
                        transNoPart.Add(row[i] + Delimiter);
                    }else {
                        transNoPart.Add(Delimiter + row[i]);
                    }                    
                }
                var mainRowPart = GetValuesByPositions(row, allColumns);
                var remainingRowPart = GetValuesByPositions(row, remaining);
                var rowToSave = transNoPart.Concat(mainRowPart.Concat(remainingRowPart));
                var line = string.Join(Delimiter, rowToSave);
                lines.Append(version);
                lines.Append(Delimiter);
                lines.Append(line);
                lines.Append(Environment.NewLine);
            }
            return lines.ToString();
        }

        private void AddIdentificationColumns(List<PrintIdFields> idFields, Dictionary<int, string> rowToSave) {
            foreach (var item in idFields) {
                if (item.AdditionalKey > -1) {
                    rowToSave[item.AdditionalKey * -1] = item.MasterAdditionalKeyVal + Delimiter + item.TestAdditionalKeyVal + Delimiter;
                }
                if (item.MainKey > -1) {
                    rowToSave[item.MainKey] = item.MainVal + Delimiter;
                }
            }
        }

        public List<string> GetValuesByPositions(string[] data, IEnumerable<int> positions) {
            var query = new List<string>();
            foreach (var item in positions) {
                query.Add(data[item]);
            }
            return query;
        }

    }
}

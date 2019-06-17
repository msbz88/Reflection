﻿using System;
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

        private string GenerateHeadersForFile(List<int> columns) {
            StringBuilder headers = new StringBuilder();
            foreach (var item in columns) {
                headers.Append(MasterHeaders[item]);
                if (item != columns.Last()) {
                    headers.Append(Delimiter);
                }
            }          
           return headers.ToString();
        }

        public void SaveComparedRows(string filePath) {
            var idFields = Data.SelectMany(row => row.IdFields.Select(col => col.Key)).Distinct().OrderBy(colId => colId).ToList();
            var allDeviationsColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList();
 
            var allColumns = idFields.Concat(allDeviationsColumns).ToList();
            var headers = GenerateHeadersForFile(allColumns);
            File.WriteAllText(filePath, headers);
            StringBuilder lines = new StringBuilder();
            StringBuilder cell = new StringBuilder();          
            foreach (var comparedRow in Data) {
                var rowToSave = CreateRowToSave(allColumns);
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
        }

        private Dictionary<int, string> CreateRowToSave(List<int> allColumns) {
            Dictionary<int, string> rowToSave = new Dictionary<int, string>(allColumns.Count);
            foreach (var colId in allColumns) {
                rowToSave.Add(colId, "0" + Delimiter);
            }
            return rowToSave;
        }

        public void SaveExtraRows(string filePath) {
            StringBuilder headers = new StringBuilder();           
            headers.Append("Version");
            foreach (var item in MasterHeaders.Data) {
                headers.Append(Delimiter);
                headers.Append(item);               
            }
            headers.Append(Environment.NewLine);
            File.WriteAllText(filePath, headers.ToString());         
            File.AppendAllText(filePath, CreateLines("Master", ExtraMaster));
            File.AppendAllText(filePath, CreateLines("Test", ExtraTest));
        }

        private string CreateLines(string version, List<string> extraLines) {
            StringBuilder lines = new StringBuilder();
            foreach (var item in extraLines) {
                lines.Append(version);
                lines.Append(Delimiter);
                lines.Append(item);
                lines.Append(Environment.NewLine);
            }
            return lines.ToString();
        }

        private void AddIdentificationColumns(Dictionary<int, string> idFields, Dictionary<int, string> rowToSave) {
            foreach (var item in idFields) {
                rowToSave[item.Key] = item.Value + Delimiter;
            }
        }


    }
}

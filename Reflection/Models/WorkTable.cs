using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class WorkTable: IWorkTable {
        public string Name { get; set; }
        public Row Headers { get; private set; }
        public List<Row> Rows { get; private set; }
        public int RowsCount { get; private set; }
        public int ColumnsCount { get; private set; }
        public char[] Delimiter { get; private set; }

        public WorkTable(string name) {
            Name = name;
            Rows = new List<Row>();
        }

        public void LoadData(IEnumerable<string> data, char[] delimiter, bool isHeadersExist, ComparisonTask comparisonTask, List<MoveColumn> correctionColumns) {
            Delimiter = delimiter;
            var firstLine = data.FirstOrDefault();
            ColumnsCount = comparisonTask.MasterConfiguration.ColumnsCount;
            if (firstLine == null || !isHeadersExist) {
                Headers = GenerateDefaultHeaders();
            }else {
                var firstRow = Parse(firstLine, correctionColumns);
                Headers = new Row(0, firstRow);
                data = data.Skip(1);
            }
            RowsCount = 0;
            foreach (var line in data) {
                comparisonTask.IfCancelRequested();
                var parsedLine = Parse(line, correctionColumns);
                if (parsedLine.Length == ColumnsCount) {
                    var row = new Row(++RowsCount, parsedLine);
                    Rows.Add(row);
                } else if (parsedLine.Length < ColumnsCount) {
                    var extendedRow = new string[ColumnsCount];
                    for (int i = 0; i < ColumnsCount; i++) {
                        if (parsedLine.Length > i) {
                            extendedRow[i] = parsedLine[i];
                        }else {
                            extendedRow[i] = "";
                        }
                    }
                    var row = new Row(++RowsCount, extendedRow);
                    Rows.Add(row);
                } else {
                    throw new Exception("Unable to parse " + RowsCount + " line with the specified delimiter. Expected " + ColumnsCount + " column(s), but got " + parsedLine.Length + ".\nTry to extract files with quoted coma or quoted semicolon delimiter.");
                }
            }
        }

        private string[] Parse(string lineToSplit, List<MoveColumn> corrections) {
            var row = Splitter.Split(lineToSplit, Delimiter);
            if (corrections.Any()) {
                int maxCorrCol = corrections.Max(item=>item.To);
                ColumnsCount = row.Length > maxCorrCol + 1 ? row.Length : maxCorrCol + 1;
                var correctedRow = Enumerable.Repeat("*null*", ColumnsCount).ToList();
                foreach (var item in corrections) {
                    if(item.From == item.To) {
                        correctedRow[item.To] = "";
                    }else {
                        correctedRow[item.To] = row[item.From];
                    }                   
                }
                for (int i = 0; i < row.Length; i++) {
                    if (correctedRow[i] == "*null*") {
                        correctedRow[i] = row[i];
                    }
                }
                return correctedRow.ToArray();
            }else {
                return row;
            }
        }

        private Row GenerateDefaultHeaders() {
            var row = new Row(0, ColumnsCount);
            for (int i = 0; i < ColumnsCount; i++) {
                row[i] = "Column" + (i + 1);
            }
            return row;
        }

        public void SaveToFile(string filePath) {
            List<string> result = new List<string>();
            var headres = "RowNum" + Delimiter + "Id" + Delimiter + string.Join(string.Join("", Delimiter), Headers);
            result.Add(headres);
            foreach (var item in Rows) {
                result.Add(item.ToString());
            }
            File.WriteAllLines(filePath, result);
        }

       public void CleanUp() {
            RowsCount = 0;
            Rows.Clear();
            Rows.TrimExcess();
        }
    }
}

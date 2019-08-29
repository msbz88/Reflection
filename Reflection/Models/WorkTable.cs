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
        public string Delimiter { get; private set; }

        public WorkTable(string name) {
            Name = name;
            Rows = new List<Row>();
        }

        public void LoadData(IEnumerable<string> data, string delimiter, bool isHeadersExist, ComparisonTask comparisonTask, List<MoveColumn> correctionColumns) {
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
            var totalLines = comparisonTask.MasterRowsCount > comparisonTask.TestRowsCount ? comparisonTask.MasterRowsCount : comparisonTask.TestRowsCount;
            foreach (var line in data) {
                comparisonTask.IfCancelRequested();
                var row = new Row(++RowsCount, Parse(line, correctionColumns));
                if (row.Data.Length == ColumnsCount) {
                    Rows.Add(row);
                    comparisonTask.UpdateProgress(10.0 / (totalLines / 0.5));
                } else {
                    throw new Exception("Unable to parse " + RowsCount + " line with the specified delimiter. Expected " + ColumnsCount + " column(s), but got " + row.Data.Length);
                }
            }
            //Rows = data.Select(line => new Row(++RowsCount, Parse(line))).ToList();         
            //RowsRep = new Dictionary<int, Row>(RowsCount);
            ////Rows = new List<Row>(RowsCount);
            //for (int i = 0; i < RowsCount; i++) {
            //    RowsRep.Add(i + 1, new Row(i + 1, ColumnsCount));
            //}
            //rowId = 0;
            //foreach (var line in data) {
            //    var splittedLine = Parse(line);
            //    Rows[rowId++].Fill(splittedLine);
            //}
        }

        private string[] Parse(string lineToSplit, List<MoveColumn> corrections) {
            var row = lineToSplit.Split(new[] { Delimiter }, StringSplitOptions.None);
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
            var headres = "RowNum" + Delimiter + "Id" + Delimiter + string.Join(Delimiter, Headers);
            result.Add(headres);
            foreach (var item in Rows) {
                result.Add(item.ToString());
            }
            File.WriteAllLines(filePath, result);
        }

       public void CleanUp() {
            RowsCount = 0;
            Rows.Clear();
        }
    }
}

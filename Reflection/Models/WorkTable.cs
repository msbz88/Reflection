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

        public void LoadData(IEnumerable<string> data, string delimiter, bool isHeadersExist, ComparisonTask comparisonTask) {
            Delimiter = delimiter;
            var firstLine = Parse(data.First());           
            ColumnsCount = firstLine.Length;
            if (isHeadersExist) {
                Headers = new Row(0, firstLine);
                data = data.Skip(1);
            } else {
                Headers = GenerateDefaultHeaders();
            }
            RowsCount = 0;
            var totalLines = comparisonTask.MasterRowsCount > comparisonTask.TestRowsCount ? comparisonTask.MasterRowsCount : comparisonTask.TestRowsCount;
            foreach (var line in data) {
                comparisonTask.IfCancelRequested();
                var row = new Row(++RowsCount, Parse(line));
                if (row.Data.Length == ColumnsCount) {
                    Rows.Add(row);
                    comparisonTask.UpdateProgress(10.0 / (totalLines / 0.5));
                } else {
                    throw new Exception("Unable to parse " + RowsCount + " line with the specified delimiter.");
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

        private string[] Parse(string lineToSplit) {
            return lineToSplit.Split(new[] { Delimiter }, StringSplitOptions.None);
        }

        private Row GenerateDefaultHeaders() {
            var row = new Row(0, ColumnsCount);
            for (int i = 0; i < ColumnsCount; i++) {
                row[i] = "Column" + i;
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

       
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class RowToSave {
        public string DefectNo { get; set; }
        public string Version { get; set; }
        public int Diff { get; set; }
        public List<string> IdFields { get; set; }
        public Dictionary<int, string> Deviations { get; set; }
        public string[] Data { get; set; }
        string Delimiter;
        StringBuilder Cell;

        public RowToSave(List<IdField> idFields, string delimiter, List<int> deviationFields) {
            IdFields = GetIdFields(idFields);
            Deviations = PrepareValues(deviationFields);
            Delimiter = delimiter;
            Cell = new StringBuilder();
            Data = new string[3 + IdFields.Count + deviationFields.Count];
        }

        public RowToSave(List<IdField> idFields) {
            IdFields = GetIdFields(idFields);
        }

        public RowToSave(List<string> idFields) {
            IdFields = idFields;
        }

        private List<string> GetIdFields(List<IdField> idFields) {
            List<string> result = new List<string>();
            var transNo = idFields.Where(item => item.MasterTransactionNoVal != null);
            foreach (var item in transNo) {
                result.Add(item.MasterTransactionNoVal);
                result.Add(item.TestTransactionNoVal);
            }
            var mainKeys = idFields.Where(item => item.MainVal != null);
            foreach (var item in mainKeys) {
                result.Add(item.MainVal);
            }
            return result;
        }

        private Dictionary<int, string> PrepareValues(List<int> deviationFields) {
            Dictionary<int, string> values = new Dictionary<int, string>(deviationFields.Count);
            foreach (var colId in deviationFields) {
                values.Add(colId, "0");
            }
            return values;
        }

        public void FillValues(List<Deviation> deviations) {
            Diff = deviations.Count;
            Version = "Master | Test";
            foreach (var deviation in deviations) {
                Cell.Clear();
                Cell.Append(deviation.MasterValue);
                Cell.Append(" | ");
                Cell.Append(deviation.TestValue);
                Deviations[deviation.ColumnId] = Cell.ToString();
            }
        }

        public void SetData() {
            Data[0] = DefectNo;
            Data[1] = Version;
            Data[2] = Diff.ToString();
            int index = 3;
            foreach (var item in IdFields) {
                Data[index++] = item;
            }
            foreach (var item in Deviations) {
                Data[index++] = item.Value;
            }
        }

        public override string ToString() {
            StringBuilder line = new StringBuilder();
            line.Append(DefectNo);
            line.Append(Version);
            line.Append(Delimiter);
            line.Append(Diff);
            foreach (var item in IdFields) {
                line.Append(Delimiter);
                line.Append(item);              
            }
            foreach (var item in Deviations) {
                line.Append(Delimiter);
                line.Append(item.Value);
            }
            line.Append(Environment.NewLine);
            return line.ToString();
        }

        public string[,] TransposeRow(List<Deviation> deviations, string[] headers) {
            Diff = deviations.Count;
            Version = "Master | Test";
            int columnsCount = 3 + IdFields.Count + 3;
            string[,] res = new string[deviations.Count, columnsCount];
            for (int row = 0; row < deviations.Count; row++) {
                int col = 0;
                res[row, col++] = DefectNo;
                res[row, col++] = Version;
                res[row, col++] = Diff.ToString();
                for (int colId = 0; colId < IdFields.Count; colId++) {
                    res[row, col++] = IdFields[colId];
                }
                res[row, col++] = headers[deviations[row].ColumnId];
                res[row, col++] = deviations[row].MasterValue;
                res[row, col++] = deviations[row].TestValue;
            }
            return res;
        }

        public string[,] TransposeExtraRow(List<string> deviations, string[] headers, string version) {
            Diff = deviations.Count;
            int columnsCount = 3 + IdFields.Count + 3;
            string[,] res = new string[deviations.Count, columnsCount];
            for (int row = 0; row < deviations.Count; row++) {
                int col = 0;
                res[row, col++] = DefectNo;
                res[row, col++] = version;
                res[row, col++] = Diff.ToString();
                for (int colId = 0; colId < IdFields.Count; colId++) {
                    res[row, col++] = IdFields[colId];
                }
                res[row, col++] = headers[row];
                if(version == "Master") {
                    res[row, col++] = deviations[row];
                }else {
                    res[row, col++ + 1] = deviations[row];
                }                
            }
            return res;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Reflection.Models {
    public class DefectsSearch {
        public bool IsEnabled{ get; private set; }
        OraSession OraSession { get; set; }
        string ProjectName { get; set; }
        double LowerVersion { get; set; }
        double UpperVersion { get; set; }
        string FoundDefect;

        public DefectsSearch() {
            IsEnabled = false;
        }

        public void Enable(string project, double lowerVersion, double upperVersion) {
            ProjectName = project;
            LowerVersion = lowerVersion;
            UpperVersion = upperVersion;
            StartSession();
            IsEnabled = true;
        }

        public string FindDefect(Dictionary<int, string> columnNames, List<BinaryValue> transNoColumns, Dictionary<int, string> idColumns, Deviation deviation) {
            FoundDefect = "";
            foreach (var transNo in transNoColumns) {
                var defectFromDB = SearchDefectByTransNo(transNo.MasterValue, transNo.TestValue, columnNames[deviation.ColumnId]);
                defectFromDB = defectFromDB == "" ? "" : "TransMatch: " + defectFromDB;
                AddDefect(defectFromDB);
            }
            if (string.IsNullOrEmpty(FoundDefect)) {
                var secCols = SearchForSecId(columnNames);
                var exSecCols = secCols.Intersect(idColumns.Select(item => item.Key));
                foreach (var col in exSecCols) {
                    var defectFromDB = SearchDefectBySecId(idColumns[col], columnNames[deviation.ColumnId]);
                    defectFromDB = defectFromDB == "" ? "" : "SecMatch: " + defectFromDB + "?";
                    AddDefect(defectFromDB);
                }
            }
            if (string.IsNullOrEmpty(FoundDefect)) {
                var defectFromDB = SearchDefectByValue(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                defectFromDB = defectFromDB == "" ? "" : "ValMatch: " + defectFromDB + "?";
                AddDefect(defectFromDB);
            }
            if (string.IsNullOrEmpty(FoundDefect)) {
                var defectFromDB = SearchDefectByValueForSameUpgrades(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                string defect = "";
                if (defectFromDB.Length > 1) {
                    defect = "UpgradeMatch (" + defectFromDB[1] + "): " + defectFromDB[0] + "?";
                }
                AddDefect(defect);
            }
            if (string.IsNullOrEmpty(FoundDefect)) {
                var defectFromDB = SearchDefectByValueInAllProjects(deviation.MasterValue, deviation.TestValue, columnNames[deviation.ColumnId]);
                string defect = "";
                if (defectFromDB.Length > 1) {
                    defect = "DeepMatch (" + defectFromDB[1] + " | " + defectFromDB[2] + "): " + defectFromDB[0] + "?";
                }
                AddDefect(defect);
            }
            return FoundDefect;
        }

        private void AddDefect(string defect) {
            if (!string.IsNullOrEmpty(defect)) {
                if (!string.IsNullOrEmpty(FoundDefect) && !FoundDefect.Contains(defect)) {
                    FoundDefect = FoundDefect + ", " + defect;
                } else {
                    FoundDefect = defect;
                }
            }
        }

        private List<int> SearchForSecId(Dictionary<int, string> columnNames) {
            return columnNames.Where(item =>
            item.Value.ToLower().Contains("secshort")
            || item.Value.ToLower().Contains("secid")
            || item.Value.ToLower().Contains("sec_id")
            ).Select(item => item.Key).ToList();
        }

        public string SearchDefectByTransNo(string masterTransNo, string testTransNo, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and Lower_Version = :lower_Version and Upper_Version = :upper_Version and Master_TransNo = :masterTransNo and Test_TransNo = :testTransNo and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":lower_Version", OracleDbType.Varchar2).Value = LowerVersion;
            cmd.Parameters.Add(":upper_Version", OracleDbType.Varchar2).Value = UpperVersion;
            cmd.Parameters.Add(":masterTransNo", OracleDbType.Varchar2).Value = masterTransNo;
            cmd.Parameters.Add(":testTransNo", OracleDbType.Varchar2).Value = testTransNo;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.OrderByDescending(item=>item.ChangedDate).FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string SearchDefectBySecId(string secId, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and Lower_Version = :lower_Version and Upper_Version = :upper_Version and SecId = :secId and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":lower_Version", OracleDbType.Varchar2).Value = LowerVersion;
            cmd.Parameters.Add(":upper_Version", OracleDbType.Varchar2).Value = UpperVersion;
            cmd.Parameters.Add(":secId", OracleDbType.Varchar2).Value = secId;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.OrderByDescending(item => item.ChangedDate).FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string SearchDefectByValue(string masterVal, string testVal, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and Lower_Version = :lower_Version and Upper_Version = :upper_Version and Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":lower_Version", OracleDbType.Varchar2).Value = LowerVersion;
            cmd.Parameters.Add(":upper_Version", OracleDbType.Varchar2).Value = UpperVersion;
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.OrderByDescending(item => item.ChangedDate).FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string[] SearchDefectByValueForSameUpgrades(string masterVal, string testVal, string columnName) {
            string query = "select * from VT_DEFECTS where Lower_Version = :lower_Version and Upper_Version = :upper_Version and Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":lower_Version", OracleDbType.Varchar2).Value = LowerVersion;
            cmd.Parameters.Add(":upper_Version", OracleDbType.Varchar2).Value = UpperVersion;
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.OrderByDescending(item => item.ChangedDate).FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? new string[] {""} : new string[] { extract.DefectNo , extract.Project};
        }

        public string[] SearchDefectByValueInAllProjects(string masterVal, string testVal, string columnName) {
            //OracleConnection oracleConnection = StartSession();
            string query = "select * from VT_DEFECTS where Lower_Version >= :lower_Version and Upper_Version <= :upper_Version and Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":lower_Version", OracleDbType.Varchar2).Value = LowerVersion;
            cmd.Parameters.Add(":upper_Version", OracleDbType.Varchar2).Value = UpperVersion;
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.OrderByDescending(item => item.ChangedDate).FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? new string[] { "" } : new string[] { extract.DefectNo, extract.Project, CreateUpgradeName(extract.LowerVersion, extract.UpperVersion) };
        }

        private string CreateUpgradeName(double lowerVersion, double upperVersion) {
            return lowerVersion + "->" + upperVersion;
        }

        private void StartSession() {
            OraSession = new OraSession("*", "*", "*", "*", "*");
            OraSession.OpenConnection();
        }

        public void Disable() {
            if (OraSession!=null) {
                OraSession.CloseConnection();
            }
            IsEnabled = false;
        }



    }
}

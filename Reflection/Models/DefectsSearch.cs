using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Reflection.Models {
    public class DefectsSearch {
        OraSession OraSession { get; set; }
        string ProjectName { get; set; }
        string UpgradeName { get; set; }

        public DefectsSearch(string project, string upgrade, OraSession oraSession) {
            ProjectName = project;
            UpgradeName = upgrade;
            OraSession = oraSession;
            //KnownDefects = GetKnownDefects(project, upgrade);
        }

        public string SearchDefectByTransNo(string masterTransNo, string testTransNo, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and UPGRADE = :upgrade and Master_TransNo = :masterTransNo and Test_TransNo = :testTransNo and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = UpgradeName;
            cmd.Parameters.Add(":masterTransNo", OracleDbType.Varchar2).Value = masterTransNo;
            cmd.Parameters.Add(":testTransNo", OracleDbType.Varchar2).Value = testTransNo;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string SearchDefectBySecId(string secId, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and UPGRADE = :upgrade and SecId = :secId and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = UpgradeName;
            cmd.Parameters.Add(":secId", OracleDbType.Varchar2).Value = secId;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string SearchDefectByValue(string masterVal, string testVal, string columnName) {
            string query = "select * from VT_DEFECTS where PROJECT = :proj and UPGRADE = :upgrade and Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = ProjectName;
            cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = UpgradeName;
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? "" : extract.DefectNo;
        }

        public string[] SearchDefectByValueForSameUpgrades(string masterVal, string testVal, string columnName) {
            string query = "select * from VT_DEFECTS where UPGRADE = :upgrade and Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = UpgradeName;
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? new string[] {""} : new string[] { extract.DefectNo , extract.Project};
        }

        public string[] SearchDefectByValueInAllProjects(string masterVal, string testVal, string columnName) {
            //OracleConnection oracleConnection = StartSession();
            string query = "select * from VT_DEFECTS where Master_Value = :masterVal and Test_Value = :testVal and Deviation_Column_Name = :columnName";
            OracleCommand cmd = new OracleCommand(query, OraSession.OracleConnection);
            cmd.Parameters.Add(":masterVal", OracleDbType.Varchar2).Value = masterVal;
            cmd.Parameters.Add(":testVal", OracleDbType.Varchar2).Value = testVal;
            cmd.Parameters.Add(":columnName", OracleDbType.Varchar2).Value = columnName;
            var extract = OraSession.AsyncGetDefectsTable(cmd).Result.FirstOrDefault();
            //OraSession.CloseConnection(oracleConnection);
            return extract == null ? new string[] { "" } : new string[] { extract.DefectNo, extract.Project, extract.Upgrade };
        }

        //private List<KnownDefect> GetKnownDefects(string project, string upgrade) {
        //    OraSession oraSession = new OraSession("DK01SV7020", "1521", "TESTIMMD", "TESTIMMD", "T7020230");
        //    oraSession.OpenConnection();
        //    var knownDefects = oraSession.AsyncGetDefectsTable(project, upgrade).Result;
        //    oraSession.CloseConnection();
        //    return knownDefects;
        //}

  

    }
}

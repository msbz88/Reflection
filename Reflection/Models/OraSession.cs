using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Reflection.Models {
    public class OraSession {
        public string Schema { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string ServiceName { get; set; }
        public OracleConnection OracleConnection { get; set; }

        public OraSession(string host, string port, string schema, string password, string serviceName) {
            Host = host;
            Port = port;
            Schema = schema;
            Password = password;
            ServiceName = serviceName;
        }

        private string CreateConnectionString() {
            return "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(" +
                   "HOST=" + Host + ")(" +
                   "PORT=" + Port + ")))(" +
                   "CONNECT_DATA=(SERVICE_NAME=" + ServiceName + "))); " +
                   "USER ID=" + Schema + "; " +
                   "PASSWORD=" + Password + ";";
        }
        
        public void OpenConnection() {
            OracleConnection = new OracleConnection { ConnectionString = CreateConnectionString() };
            OracleConnection.Open();
        }

        public void CloseConnection() {
            OracleConnection.Close();
            OracleConnection.Dispose();
        }

        private List<KnownDefect> ExecuteQuery(OracleCommand cmd) {
            List<KnownDefect> result = new List<KnownDefect>();
            cmd.CommandType = CommandType.Text;
            using (OracleDataReader dataAdapter = cmd.ExecuteReader()) {
                while (dataAdapter.Read()) {
                    var knownDefect = new KnownDefect(
                        dataAdapter.IsDBNull(0) ? "" : dataAdapter.GetString(0),
                        dataAdapter.GetDouble(1),
                        dataAdapter.GetDouble(2),
                        dataAdapter.IsDBNull(3) ? "" : dataAdapter.GetString(3),
                        dataAdapter.IsDBNull(4) ? "" : dataAdapter.GetString(4),
                        dataAdapter.IsDBNull(5) ? "" : dataAdapter.GetString(5),
                        dataAdapter.IsDBNull(6) ? "" : dataAdapter.GetString(6),
                        dataAdapter.IsDBNull(7) ? "" : dataAdapter.GetString(7),
                        dataAdapter.IsDBNull(8) ? "" : dataAdapter.GetString(8),
                        dataAdapter.IsDBNull(9) ? "" : dataAdapter.GetString(9),
                        dataAdapter.GetDateTime(10)
                        );
                    result.Add(knownDefect);
                }
            }
            return result;
        }

        public void InsertIntoLogTable(ComparisonTask comparisonTask, string userId, Dictionary<int, string> numberedColumnNames, bool isUserKey) {
            string query = "INSERT INTO VT_comp_app_log(userID, StartTime, MasterFilePath, TestFilePath, IsLinearView, IsDeviationOnly, MasterRowsCount, TestRowsCount, ActualRowsDiff, RowsWithDeviations, ExtraMasterCount, ExtraTestCount, Time, Status, Progress, IsUserKey, CompKey, ExcludedColumns, IdColumns, ErrorMessage, projectName, upgrade) " +
                "VALUES(:userID, :startTime, :masterFilePath, :testFilePath, :isLinearView, :isDeviationOnly, :masterRowsCount, :testRowsCount, :actualRowsDiff, :rowsWithDeviations, :extraMasterCount, :extraTestCount, :time, :status, :progress, :isUserKey, :compKey, :excludedColumns, :idColumns, :errorMessage, :projectName, :upgrade)";
            OracleCommand cmd = new OracleCommand(query, OracleConnection);
            cmd.Parameters.Add(":userID", OracleDbType.Varchar2).Value = userId;
            cmd.Parameters.Add(":startTime", OracleDbType.TimeStamp).Value = DateTime.Parse(comparisonTask.StartTime);
            cmd.Parameters.Add(":masterFilePath", OracleDbType.Varchar2).Value = comparisonTask.MasterConfiguration.FilePath;
            cmd.Parameters.Add(":testFilePath", OracleDbType.Varchar2).Value = comparisonTask.TestConfiguration.FilePath;
            cmd.Parameters.Add(":isLinearView", OracleDbType.Varchar2).Value = comparisonTask.IsLinearView;
            cmd.Parameters.Add(":isDeviationOnly", OracleDbType.Varchar2).Value = comparisonTask.IsDeviationsOnly;
            cmd.Parameters.Add(":masterRowsCount", OracleDbType.Int32).Value = comparisonTask.MasterRowsCount;
            cmd.Parameters.Add(":testRowsCount", OracleDbType.Int32).Value = comparisonTask.TestRowsCount;
            cmd.Parameters.Add(":actualRowsDiff", OracleDbType.Int32).Value = comparisonTask.ActualRowsDiff;
            cmd.Parameters.Add(":rowsWithDeviations", OracleDbType.Int32).Value = comparisonTask.RowsWithDeviations;
            cmd.Parameters.Add(":extraMasterCount", OracleDbType.Int32).Value = comparisonTask.ExtraMasterCount;
            cmd.Parameters.Add(":extraTestCount", OracleDbType.Int32).Value = comparisonTask.ExtraTestCount;
            cmd.Parameters.Add(":time", OracleDbType.Varchar2).Value = comparisonTask.ElapsedTime;
            cmd.Parameters.Add(":status", OracleDbType.Varchar2).Value = comparisonTask.Status;
            cmd.Parameters.Add(":progress", OracleDbType.Varchar2).Value = comparisonTask.Progress > 100 ? 100 : comparisonTask.Progress;
            cmd.Parameters.Add(":isUserKey", OracleDbType.Varchar2).Value = isUserKey;
            cmd.Parameters.Add(":compKey", OracleDbType.Varchar2).Value = string.Join("; ", numberedColumnNames.Where(item=> comparisonTask.ComparisonKeys.MainKeys.Contains(item.Key)).Select(item=> item.Value));
            cmd.Parameters.Add(":excludedColumnss", OracleDbType.Varchar2).Value = string.Join("; ", numberedColumnNames.Where(item => comparisonTask.ComparisonKeys.ExcludeColumns.Contains(item.Key)).Select(item => item.Value));
            cmd.Parameters.Add(":idColumns", OracleDbType.Varchar2).Value = string.Join("; ", numberedColumnNames.Where(item => comparisonTask.ComparisonKeys.SingleIdColumns.Concat(comparisonTask.ComparisonKeys.BinaryIdColumns).Contains(item.Key)).Select(item => item.Value));
            cmd.Parameters.Add(":errorMessage", OracleDbType.Varchar2).Value = comparisonTask.ErrorMessage;
            cmd.Parameters.Add(":projectName", OracleDbType.Varchar2).Value = comparisonTask.ProjectName;
            string upgrade = "";
            if (comparisonTask.UpgradeVersions != null) {
                upgrade = string.Join("->", comparisonTask.UpgradeVersions);
            }
            cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = upgrade;
            cmd.ExecuteNonQuery();
        }

        private Task<List<KnownDefect>> ExecuteQueryParallel(OracleCommand cmd) {
            return Task.Run(() => ExecuteQuery(cmd));
        }

        public async Task<List<KnownDefect>> AsyncGetDefectsTable(OracleCommand cmd) {
            return await ExecuteQueryParallel(cmd);
        }



    }
}

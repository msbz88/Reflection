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

        private Task<List<KnownDefect>> ExecuteQueryParallel(OracleCommand cmd) {
            return Task.Run(() => ExecuteQuery(cmd));
        }

        public async Task<List<KnownDefect>> AsyncGetDefectsTable(OracleCommand cmd) {
            //string query = "select * from VT_DEFECTS where PROJECT = :proj and UPGRADE = :upgrade";
            //OracleCommand cmd = new OracleCommand(query, OracleConnection);
            //cmd.Parameters.Add(":proj", OracleDbType.Varchar2).Value = proj;
            //cmd.Parameters.Add(":upgrade", OracleDbType.Varchar2).Value = upgrade;
            return await ExecuteQueryParallel(cmd);
        }



    }
}

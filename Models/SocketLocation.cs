using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace SocketServerApp.Models
{
    public class SocketLocation
    {
        public string ip_address { get; set; }
        public string location { get; set; }
        public string readerno { get; set; }
        public string pointname { get; set; }
    }
    public class SocketLocationManage
    {
        private ProgramMethod PGMethod = new ProgramMethod();
        public List<SocketLocation> GetSetting(SqlConnection Comm)
        {
            string SelectSQL = @"SELECT ip_address, location, readerno, pointname FROM SocketLocation ";
            bool ReturnFlage = false;
            List<SocketLocation> ReaderInfo = new List<SocketLocation>();
            SqlCommand cmd = new SqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                SqlDataReader dataReader = cmd.ExecuteReader();
                ReturnFlage = dataReader.HasRows;
                if (ReturnFlage)
                {

                    ReaderInfo.Clear();
                    while (dataReader.Read())
                    {
                        SocketLocation SLdata = new SocketLocation();
                        SLdata.ip_address = dataReader.GetString(0);
                        SLdata.location = dataReader.GetString(1);
                        SLdata.readerno = dataReader.GetString(2);
                        SLdata.pointname = dataReader.GetString(3);
                        ReaderInfo.Add(SLdata);
                    }
                }
                cmd.Cancel();
                Comm.Close();
                return ReaderInfo;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("取得Socket Location設定清冊失敗：" + ex.Message);
                ReaderInfo.Clear();
                return ReaderInfo;
            }
        }
        public List<SocketLocation> GetMysqlSetting(MySqlConnection Comm)
        {
            string SelectSQL = @"SELECT ip_address, location, readerno, pointname FROM SocketLocation ";
            bool ReturnFlage = false;
            List<SocketLocation> ReaderInfo = new List<SocketLocation>();
            MySqlCommand cmd = new MySqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                MySqlDataReader dataReader = cmd.ExecuteReader();
                ReturnFlage = dataReader.HasRows;
                if (ReturnFlage)
                {

                    ReaderInfo.Clear();
                    while (dataReader.Read())
                    {
                        SocketLocation SLdata = new SocketLocation();
                        SLdata.ip_address = dataReader.GetString(0);
                        SLdata.location = dataReader.GetString(1);
                        SLdata.readerno = dataReader.GetString(2);
                        SLdata.pointname = dataReader.GetString(3);
                        ReaderInfo.Add(SLdata);
                    }
                }
                cmd.Cancel();
                Comm.Close();
                return ReaderInfo;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("取得Socket Location設定清冊失敗：" + ex.Message);
                ReaderInfo.Clear();
                return ReaderInfo;
            }
        }
    }
}

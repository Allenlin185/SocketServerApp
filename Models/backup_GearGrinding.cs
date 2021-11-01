using MySql.Data.MySqlClient;
using System;
using System.Data.SqlClient;

namespace SocketServerApp.Models
{
    public class backup_GearGrinding
    {
        public string product_id { get; set; }
        public DateTime qctime { get; set; }
        public string reader_name { get; set; }
        public string reader_ip { get; set; }
        public string point_name { get; set; }
    }
    public class backup_GearGrindingManage
    {
        private backup_ProgramMethod PGMethod = new backup_ProgramMethod();
        public bool InsertTable(backup_GearGrinding GGData, SqlConnection Comm)
        {
            string InsertSQL = @"insert into geargrinding (product_id, qctime, reader_name, reader_ip, point_name) 
                values (@product_id, @qctime, @reader_name, @reader_ip, @point_name)";
            try
            {
                Comm.Open();
                SqlCommand cmd = new SqlCommand(InsertSQL, Comm);
                SqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@product_id", GGData.product_id);
                cmd.Parameters.AddWithValue("@qctime", GGData.qctime);
                cmd.Parameters.AddWithValue("@reader_name", GGData.reader_name);
                cmd.Parameters.AddWithValue("@reader_ip", GGData.reader_ip);
                cmd.Parameters.AddWithValue("@point_name", GGData.point_name);
                cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("新增GG資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool CheckProductExist(backup_GearGrinding GG, SqlConnection Comm)
        {
            string SelectSQL = @"SELECT product_id FROM geargrinding WHERE product_id =@product_id and reader_name =@reader_name and point_name = @point_name";
            bool ReturnFlage = false;
            SqlCommand cmd = new SqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                cmd.Parameters.AddWithValue("@product_id", GG.product_id);
                cmd.Parameters.AddWithValue("@reader_name", GG.reader_name);
                cmd.Parameters.AddWithValue("@point_name", GG.point_name);
                SqlDataReader dataReader = cmd.ExecuteReader();
                ReturnFlage = dataReader.HasRows;
                cmd.Cancel();
                Comm.Close();
                return ReturnFlage;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("查詢GG 物料編號失敗：" + ex.Message);
                return false;
            }
        }
        public bool UpdateTable(backup_GearGrinding GGData, SqlConnection Comm)
        {
            string UpdateSQL = @"UPDATE geargrinding SET qctime = @qctime, reader_ip = @reader_ip WHERE product_id = @product_id and reader_name = @reader_name and point_name = @point_name";
            try
            {
                Comm.Open();
                SqlCommand cmd = new SqlCommand(UpdateSQL, Comm);
                SqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@qctime", GGData.qctime);
                cmd.Parameters.AddWithValue("@reader_ip", GGData.reader_ip);
                cmd.Parameters.AddWithValue("@product_id", GGData.product_id);
                cmd.Parameters.AddWithValue("@reader_name", GGData.reader_name);
                cmd.Parameters.AddWithValue("@point_name", GGData.point_name);
                cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("更新GG資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool InsertTableMysql(backup_GearGrinding GGData, MySqlConnection Comm)
        {
            string InsertSQL = @"insert into geargrinding (product_id, qctime, reader_name, reader_ip, point_name) 
                values (@product_id, @qctime, @reader_name, @reader_ip, @point_name)";
            try
            {
                Comm.Open();
                //SqlCommand cmd = new MySqlCommand(Comm);
                var command = Comm.CreateCommand();
                command.CommandText = InsertSQL;

                MySqlTransaction trans = Comm.BeginTransaction();
                command.Transaction = trans;
                command.Parameters.AddWithValue("@product_id", GGData.product_id);
                command.Parameters.AddWithValue("@qctime", GGData.qctime);
                command.Parameters.AddWithValue("@reader_name", GGData.reader_name);
                command.Parameters.AddWithValue("@reader_ip", GGData.reader_ip);
                command.Parameters.AddWithValue("@point_name", GGData.point_name);
                command.ExecuteNonQueryAsync();
                trans.Commit();
                command.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("新增GG資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool CheckProductMysql(backup_GearGrinding GGData, MySqlConnection Comm)
        {
            string SelectSQL = @"SELECT product_id FROM geargrinding WHERE product_id =@product_id and reader_name =@reader_name and point_name = @point_name";
            MySqlCommand cmd = new MySqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                cmd.Parameters.AddWithValue("@product_id", GGData.product_id);
                cmd.Parameters.AddWithValue("@reader_name", GGData.reader_name);
                cmd.Parameters.AddWithValue("@point_name", GGData.point_name);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool ReturnFlage = dataReader.HasRows;
                cmd.Cancel();
                Comm.Close();
                return ReturnFlage;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("查詢GG 物料編號失敗：" + ex.Message);
                return false;
            }
        }
        public bool UpdateTableMysql(backup_GearGrinding GGData, MySqlConnection Comm)
        {
            string UpdateSQL = @"UPDATE geargrinding SET qctime = @qctime, reader_ip = @reader_ip WHERE product_id = @product_id and reader_name = @reader_name and point_name = @point_name";
            try
            {
                Comm.Open();
                MySqlCommand cmd = new MySqlCommand(UpdateSQL, Comm);
                MySqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@qctime", GGData.qctime);
                cmd.Parameters.AddWithValue("@reader_ip", GGData.reader_ip);
                cmd.Parameters.AddWithValue("@product_id", GGData.product_id);
                cmd.Parameters.AddWithValue("@reader_name", GGData.reader_name);
                cmd.Parameters.AddWithValue("@point_name", GGData.point_name);
                cmd.ExecuteNonQueryAsync();
                //cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("更新GG資料失敗：" + ex.Message);
                return false;
            }
        }
    }
}

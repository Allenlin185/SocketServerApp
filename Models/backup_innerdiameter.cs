using MySql.Data.MySqlClient;
using System;
using System.Data.SqlClient;

namespace SocketServerApp.Models
{
    public class backup_innerdiameter
    {
        public string product_id { get; set; }
        public DateTime qctime { get; set; }
        public string qc_result { get; set; }
        public string q1result { get; set; }
        public Decimal q1measured { get; set; }
        public Decimal q1maxvalue { get; set; }
        public string q2result { get; set; }
        public Decimal q2measured { get; set; }
        public Decimal q2maxvalue { get; set; }
        public string q3result { get; set; }
        public Decimal q3measured { get; set; }
        public Decimal q3thresmax { get; set; }
        public Decimal q3thresmin { get; set; }
        public string q4result { get; set; }
        public Decimal q4measured { get; set; }
        public Decimal q4maxvalue { get; set; }
        public string machine_no { get; set; }
        public string ld_operator { get; set; }
    }
    public class backup_InnerDiameterManage
    {
        private backup_ProgramMethod PGMethod = new backup_ProgramMethod();
        public bool InsertTable(backup_innerdiameter IGData, SqlConnection Comm)
        {
            string InsertSQL = @"insert into innerdiameter (qctime, product_id, qc_result, q1result, q1measured, q1maxvalue, q2result, q2measured, q2maxvalue, q3result, q3measured, 
                q3thresmax, q3thresmin, q4result, q4measured, q4maxvalue, machine_no, ld_operator) values (@qctime, @product_id, @qc_result, @q1result, @q1measured, @q1maxvalue, 
                @q2result, @q2measured, @q2maxvalue, @q3result, @q3measured, @q3thresmax, @q3thresmin, @q4result, @q4measured, @q4maxvalue, @machine_no, @ld_operator)";
            try
            {
                Comm.Open();
                SqlCommand cmd = new SqlCommand(InsertSQL, Comm);
                SqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@qctime", IGData.qctime);
                cmd.Parameters.AddWithValue("@product_id", IGData.product_id);
                cmd.Parameters.AddWithValue("@qc_result", IGData.qc_result);
                cmd.Parameters.AddWithValue("@q1result", IGData.q1result);
                cmd.Parameters.AddWithValue("@q1measured", IGData.q1measured);
                cmd.Parameters.AddWithValue("@q1maxvalue", IGData.q1maxvalue);
                cmd.Parameters.AddWithValue("@q2result", IGData.q2result);
                cmd.Parameters.AddWithValue("@q2measured", IGData.q2measured);
                cmd.Parameters.AddWithValue("@q2maxvalue", IGData.q2maxvalue);
                cmd.Parameters.AddWithValue("@q3result", IGData.q3result);
                cmd.Parameters.AddWithValue("@q3measured", IGData.q3measured);
                cmd.Parameters.AddWithValue("@q3thresmax", IGData.q3thresmax);
                cmd.Parameters.AddWithValue("@q3thresmin", IGData.q3thresmin);
                cmd.Parameters.AddWithValue("@q4result", IGData.q4result);
                cmd.Parameters.AddWithValue("@q4measured", IGData.q4measured);
                cmd.Parameters.AddWithValue("@q4maxvalue", IGData.q4maxvalue);
                cmd.Parameters.AddWithValue("@machine_no", IGData.machine_no);
                cmd.Parameters.AddWithValue("@ld_operator", IGData.ld_operator);
                cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("新增內研量測資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool CheckProductExist(backup_innerdiameter IGData, SqlConnection Comm)
        {
            string SelectSQL = @"SELECT qctime, product_id FROM innerdiameter WHERE product_id =@product_id";
            bool ReturnFlage = false;
            SqlCommand cmd = new SqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                cmd.Parameters.AddWithValue("@product_id", IGData.product_id);
                SqlDataReader dataReader = cmd.ExecuteReader();
                ReturnFlage = dataReader.HasRows;
                cmd.Cancel();
                Comm.Close();
                return ReturnFlage;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("查詢內研加工物料編號失敗：" + ex.Message);
                return false;
            }
        }
        public bool UpdateTable(backup_innerdiameter IGData, SqlConnection Comm)
        {
            string UpdateSQL = @"UPDATE innerdiameter SET qctime = @qctime, qc_result = @qc_result, q1result = @q1result, q1measured = @q1measured, 
                q1maxvalue = @q1maxvalue, q2result = @q2result, q2measured = @q2measured, q2maxvalue = @q2maxvalue, q3result = @q3result, q3measured = @q3measured, 
                q3thresmax = @q3thresmax, q3thresmin = @q3thresmin, q4result = @q4result, q4measured = @q4measured, q4maxvalue = @q4maxvalue, machine_no = @machine_no, 
                ld_operator = @ld_operator WHERE product_id = @product_id ";
            try
            {
                Comm.Open();
                SqlCommand cmd = new SqlCommand(UpdateSQL, Comm);
                SqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@qctime", IGData.qctime);
                cmd.Parameters.AddWithValue("@qc_result", IGData.qc_result);
                cmd.Parameters.AddWithValue("@q1result", IGData.q1result);
                cmd.Parameters.AddWithValue("@q1measured", IGData.q1measured);
                cmd.Parameters.AddWithValue("@q1maxvalue", IGData.q1maxvalue);
                cmd.Parameters.AddWithValue("@q2result", IGData.q2result);
                cmd.Parameters.AddWithValue("@q2measured", IGData.q2measured);
                cmd.Parameters.AddWithValue("@q2maxvalue", IGData.q2maxvalue);
                cmd.Parameters.AddWithValue("@q3result", IGData.q3result);
                cmd.Parameters.AddWithValue("@q3measured", IGData.q3measured);
                cmd.Parameters.AddWithValue("@q3thresmax", IGData.q3thresmax);
                cmd.Parameters.AddWithValue("@q3thresmin", IGData.q3thresmin);
                cmd.Parameters.AddWithValue("@q4result", IGData.q4result);
                cmd.Parameters.AddWithValue("@q4measured", IGData.q4measured);
                cmd.Parameters.AddWithValue("@q4maxvalue", IGData.q4maxvalue);
                cmd.Parameters.AddWithValue("@machine_no", IGData.machine_no);
                cmd.Parameters.AddWithValue("@ld_operator", IGData.ld_operator);
                cmd.Parameters.AddWithValue("@product_id", IGData.product_id);
                cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("更新內研量測資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool InsertTableMysql(backup_innerdiameter IGData, MySqlConnection Comm)
        {
            string InsertSQL = @"insert into igeqdata (cID, cDateTime, cResult, cComment) values (@cID, @cDateTime, @cResult, @cComment)";
            try
            {
                Comm.Open();
                //SqlCommand cmd = new MySqlCommand(Comm);
                var command = Comm.CreateCommand();
                command.CommandText = InsertSQL;

                MySqlTransaction trans = Comm.BeginTransaction();
                command.Transaction = trans;
                command.Parameters.AddWithValue("@cID", IGData.product_id);
                command.Parameters.AddWithValue("@cDateTime", IGData.qctime);
                command.Parameters.AddWithValue("@cResult", IGData.qc_result);
                command.Parameters.AddWithValue("@cComment", "");
                command.ExecuteNonQueryAsync();
                trans.Commit();
                command.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("新增內研量測資料失敗：" + ex.Message);
                return false;
            }
        }
        public bool CheckProductMysql(backup_innerdiameter IGData, MySqlConnection Comm)
        {
            string SelectSQL = @"SELECT cID FROM igeqdata WHERE cID =@cID";
            MySqlCommand cmd = new MySqlCommand(SelectSQL, Comm);
            try
            {
                Comm.Open();
                cmd.Parameters.AddWithValue("@cID", IGData.product_id);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool ReturnFlage = dataReader.HasRows;
                cmd.Cancel();
                Comm.Close();
                return ReturnFlage;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("查詢內研加工物料編號失敗：" + ex.Message);
                return false;
            }
        }
        public bool UpdateTableMysql(backup_innerdiameter IGData, MySqlConnection Comm)
        {
            string UpdateSQL = @"UPDATE igeqdata SET cDateTime = @cDateTime, cResult = @cResult WHERE cID = @cID";
            try
            {
                Comm.Open();
                MySqlCommand cmd = new MySqlCommand(UpdateSQL, Comm);
                MySqlTransaction trans = Comm.BeginTransaction();
                cmd.Transaction = trans;
                cmd.Parameters.AddWithValue("@cDateTime", IGData.qctime);
                cmd.Parameters.AddWithValue("@cResult", IGData.qc_result);
                cmd.Parameters.AddWithValue("@cID", IGData.product_id);
                cmd.ExecuteNonQueryAsync();
                //cmd.ExecuteNonQuery();
                trans.Commit();
                cmd.Cancel();
                Comm.Close();
                return true;
            }
            catch (Exception ex)
            {
                PGMethod.WriteLog("更新內研量測資料失敗：" + ex.Message);
                return false;
            }
        }
    }
}

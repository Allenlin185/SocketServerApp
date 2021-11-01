using System;
using System.IO;

namespace SocketServerApp.Models
{
    class backup_ProgramMethod
    {
        public void WriteLog(string logMsg)
        {
            string logFilePath = "Log";
            if (!Directory.Exists(logFilePath))
            {
                Directory.CreateDirectory(logFilePath);
            }
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            string pathString = Path.Combine(logFilePath, logFileName);
            if (!File.Exists(pathString))
            {
                FileStream fs = File.Create(pathString);
                fs.Close();
            }
            string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            StreamWriter sw = File.AppendText(pathString);
            sw.WriteLine(nowTime + ": " + logMsg);
            sw.Close();
        }
        public void WriteGGData(backup_GearGrinding GGData, string Repeat = "N")
        {
            string DataFilePath = "DATA";
            DataFilePath = Path.Combine(DataFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(DataFilePath))
            {
                Directory.CreateDirectory(DataFilePath);
            }
            string dataFileName = GGData.reader_ip + ".csv";
            string pathString = Path.Combine(DataFilePath, dataFileName);
            if (!File.Exists(pathString))
            {
                FileStream fs = File.Create(pathString);
                fs.Close();
            }
            string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            StreamWriter sw = File.AppendText(pathString);
            string DataMsg;
            if (Repeat == "D")
            {
                DataMsg = nowTime + "," + GGData.reader_name + "," + GGData.product_id + "," + GGData.point_name + "," + Repeat;
            }
            else
            {
                DataMsg = nowTime + "," + GGData.reader_name + "," + GGData.product_id + "," + GGData.point_name + ", ";
            }

            sw.WriteLine(DataMsg);
            sw.Close();
        }
        public void WriteFQCData(string ipaddress, string recv)
        {
            string DataFilePath = "DATA";
            DataFilePath = Path.Combine(DataFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(DataFilePath))
            {
                Directory.CreateDirectory(DataFilePath);
            }
            string dataFileName = ipaddress + ".txt";
            string pathString = Path.Combine(DataFilePath, dataFileName);
            if (!File.Exists(pathString))
            {
                FileStream fs = File.Create(pathString);
                fs.Close();
            }
            string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            StreamWriter sw = File.AppendText(pathString);
            sw.WriteLine(recv);
            sw.Close();
        }
        public void WriteIGData(backup_innerdiameter IGData)
        {
            string DataFilePath = "DATA";
            DataFilePath = Path.Combine(DataFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(DataFilePath))
            {
                Directory.CreateDirectory(DataFilePath);
            }
            string dataFileName = IGData.machine_no + ".csv";
            string pathString = Path.Combine(DataFilePath, dataFileName);
            if (!File.Exists(pathString))
            {
                FileStream fs = File.Create(pathString);
                fs.Close();
            }
            StreamWriter sw = File.AppendText(pathString);
            string DataMsg = IGData.product_id + "," + IGData.qctime + "," + IGData.qc_result + ",";
            sw.WriteLine(DataMsg);
            sw.Close();
        }
    }
}

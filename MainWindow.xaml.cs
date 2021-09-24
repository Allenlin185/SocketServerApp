using SocketServerApp.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MySql.Data;
using MySql.Data.MySqlClient;
using MessageBox = System.Windows.MessageBox;

namespace SocketServerApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private ProgramMethod PGMethod = new ProgramMethod();
        private SqlConnection sqlConnection;
        private MySqlConnection MysqlConnection;
        private GearGrinding GGData = new GearGrinding();
        private GearGrindingManage GGManage;
        private SocketLocationManage SLManage = new SocketLocationManage();
        private List<SocketLocation> ReaderInfo = new List<SocketLocation>();

        private Socket hotaserver;
        private IPAddress iPAddress;
        private int port;
        private List<Socket> ConnectSockeList = new List<Socket>();
        private Dictionary<int, List<Thread>> recvThreadList = new Dictionary<int, List<Thread>>();
        public MainWindow()
        {
            InitializeComponent();
            Initial();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    cb_ipaddress.Items.Add(ip.ToString());
                }
            }
        }
        private void Initial()  //  Click右下角功能圖示事件
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipText = "正在處理中...";
            notifyIcon.Text = "Socket服務啟動";
            notifyIcon.Icon = new Icon(@"hotalogo.ico");
            notifyIcon.Click += (sender1, ex) =>
            {
                ShowInTaskbar = true;
                Show();
            };
        }
        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            sv_log.Content = "";
            int n;
            if (!int.TryParse(tb_port.Text, out n) || int.Parse(tb_port.Text) <= 1024)
            {
                sv_log.Content += "Port 請輸入大於1024的數字。\n";
                return;
            }
            if (cb_ipaddress.SelectedIndex > -1)
            {
                sv_log.Content += cb_ipaddress.SelectedItem.ToString();
            }
            else
            {
                sv_log.Content += "請選擇IP Address.";
                return;
            }

            string strConn = "Data Source=10.3.30.203;Database=hotaMeasure;User Id=sa;Password=hota;";
            string strmysqlConn = "server=127.0.0.1;uid=root;pwd=hota168;database=testMeasure";
            if (cb_database.SelectedIndex == 0)
            {
                try
                {
                    sqlConnection = new SqlConnection(strConn);
                    
                    ReaderInfo = SLManage.GetSetting(sqlConnection);
                    if (ReaderInfo.Count == 0)
                    {
                        MessageBox.Show("連線DB失敗或無設定資料，請連絡資訊人員。");
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    PGMethod.WriteLog("連線DB失敗：" + ex.Message);
                    MessageBox.Show("連線DB失敗，請連絡資訊人員。");
                }
            }
            else
            {
                try
                {
                    MysqlConnection = new MySqlConnection(strmysqlConn);
                    ReaderInfo = SLManage.GetMysqlSetting(MysqlConnection);
                    if (ReaderInfo.Count == 0)
                    {
                        MessageBox.Show("連線DB失敗或無設定資料，請連絡資訊人員。");
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    PGMethod.WriteLog("連線DB失敗：" + ex.Message);
                    MessageBox.Show("連線DB失敗，請連絡資訊人員。");
                }
            }
            

            btn_start.Visibility = Visibility.Hidden;
            btn_stop.Visibility = Visibility.Visible;
            ShowInTaskbar = false;
            Hide();
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(1000, "程式說明", notifyIcon.Text, ToolTipIcon.Info);
            iPAddress = IPAddress.Parse(cb_ipaddress.SelectedItem.ToString());
            port = int.Parse(tb_port.Text);
            CreateSocket();
            if (BindAndListen())
            {
                WaitClientConnection();
            }

        }
        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void CreateSocket()
        {
            try
            {
                hotaserver = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception)
            {
                sv_log.Content = "創立Socket失敗!\n" + sv_log.Content;
            }
        }
        private bool BindAndListen()
        {
            try
            {
                hotaserver.Bind(new IPEndPoint(iPAddress, port));
                hotaserver.Listen(100);
                return true;
            }
            catch (Exception)
            {
                PGMethod.WriteLog("設定PORT:" + port.ToString() + " 發生衝突！");
                return false;
            }
        }
        private void WaitClientConnection()
        {
            int Index = 0;
            while (true)
            {
                PGMethod.WriteLog("當前連結數量：" + recvThreadList.Count);
                PGMethod.WriteLog("等待客戶端的連結：");
                Socket ClientSocket = hotaserver.Accept();
                
                if (ClientSocket != null)
                {
                    foreach (SocketLocation SLRow in ReaderInfo)
                    {
                        string remoteip = ClientSocket.RemoteEndPoint.ToString();
                        string[] words = remoteip.Split(':');
                        if (words[0] == SLRow.ip_address)
                        {
                            PGMethod.WriteLog(ClientSocket.RemoteEndPoint + " 連線成功！");
                            ConnectSockeList.Add(ClientSocket);
                            Thread recv;
                            Thread send = new Thread(SendMessage);
                            int DBtype = cb_database.SelectedIndex;
                            switch (SLRow.location)
                            {
                                case "GG":
                                    recv = new Thread(RecvGGMessage);
                                    recv.Start(new ArrayList { Index, ClientSocket, DBtype });
                                    send.Start(new ArrayList { Index, ClientSocket });
                                    recvThreadList.Add(Index, new List<Thread> { recv, send });
                                    break;
                                case "FQC":
                                    recv = new Thread(RecvFQCMessage);
                                    recv.Start(new ArrayList { Index, ClientSocket, DBtype });
                                    send.Start(new ArrayList { Index, ClientSocket });
                                    recvThreadList.Add(Index, new List<Thread> { recv, send });
                                    break;
                                case "QC2":
                                    recv = new Thread(RecvFQCMessage);
                                    recv.Start(new ArrayList { Index, ClientSocket, DBtype });
                                    send.Start(new ArrayList { Index, ClientSocket });
                                    recvThreadList.Add(Index, new List<Thread> { recv, send });
                                    break;
                                case "QC1":
                                    recv = new Thread(RecvFQCMessage);
                                    recv.Start(new ArrayList { Index, ClientSocket, DBtype });
                                    send.Start(new ArrayList { Index, ClientSocket });
                                    recvThreadList.Add(Index, new List<Thread> { recv, send });
                                    break;
                            }
                        }
                        else
                        {
                            PGMethod.WriteLog(ClientSocket.RemoteEndPoint + " IP不存在資料表中！");
                        }
                    }
                    /*
                    Thread send = new Thread(SendMessage);
                    send.Start(new ArrayList { Index, ClientSocket });
                    recvThreadList.Add(Index, new List<Thread> { recv, send });
                    */
                    Index++;
                }
            }
        }
        private void RecvGGMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
            int DBtype = (int)arraylist[2];
            GGManage = new GearGrindingManage();
            while (true)
            {
                try
                {
                    byte[] strbyte = new byte[2048];
                    int count = clientsocket.Receive(strbyte);
                    string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                    foreach (SocketLocation SLRow in ReaderInfo)
                    {
                        string remoteip = clientsocket.RemoteEndPoint.ToString();
                        string[] words = remoteip.Split(':');
                        if (words[0] == SLRow.ip_address)
                        {
                            GGData.product_id = ret.Trim();
                            GGData.qctime = DateTime.Now;
                            GGData.reader_name = SLRow.readerno;
                            GGData.reader_ip = SLRow.ip_address;
                            GGData.point_name = SLRow.pointname;
                            
                            if (DBtype == 0)
                            {
                                if (GGManage.CheckProductExist(GGData, sqlConnection))
                                {
                                    GGManage.UpdateTable(GGData, sqlConnection);
                                    PGMethod.WriteGGData(GGData, "D");
                                }
                                else
                                {
                                    GGManage.InsertTable(GGData, sqlConnection);
                                    PGMethod.WriteGGData(GGData);
                                }
                            }
                            else
                            {
                                if (GGManage.CheckProductMysql(GGData, MysqlConnection))
                                {
                                    GGManage.UpdateTableMysql(GGData, MysqlConnection);
                                    PGMethod.WriteGGData(GGData, "D");
                                }
                                else
                                {
                                    GGManage.InsertTableMysql(GGData, MysqlConnection);
                                    PGMethod.WriteGGData(GGData);
                                }
                            }
                            break;
                        }
                    }
                }
                catch (Exception recv)
                {
                    PGMethod.WriteLog(recv.Message);
                    recvThreadList[index][1].Abort();
                    recvThreadList.Remove(index);
                }
            }
        }
        private void RecvFQCMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
            int DBtype = (int)arraylist[2];
            while (true)
            {
                try
                {
                    byte[] strbyte = new byte[2048];
                    int count = clientsocket.Receive(strbyte);
                    string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                    
                    string remoteip = clientsocket.RemoteEndPoint.ToString();
                    string[] words = remoteip.Split(':');
                    PGMethod.WriteFQCData(words[0], ret);
                }
                catch (Exception recv)
                {
                    PGMethod.WriteLog(recv.Message);
                    recvThreadList[index][0].Abort();
                    recvThreadList.Remove(index);
                }
            }
        }
        private void SendMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
            while (true)
            {
                try
                {
                    byte[] strbyte = Encoding.ASCII.GetBytes("Ok");
                    clientsocket.Send(strbyte);
                }
                catch (Exception send)
                {
                    PGMethod.WriteLog(send.Message);
                    recvThreadList[index][1].Abort();
                    recvThreadList.Remove(index);
                    //recvThreadList.Remove(index);
                }
            }
        }
    }
}

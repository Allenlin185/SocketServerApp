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
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using XMLMethod;

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
        #region 舊程式
        /*
        private GearGrinding GGData;
        private GearGrindingManage GGManage;
        private innerdiameter IGData;
        private InnerDiameterManage IGManage;
        */
        #endregion
        private SocketLocationManage SLManage = new SocketLocationManage();
        private List<SocketLocation> ReaderInfo = new List<SocketLocation>();
        private Socket hotaserver;
        private int SckCIndex;
        private int RDataLen = 2048;
        private IPAddress iPAddress;
        private int port;
        private List<Socket> ConnectSockeList = new List<Socket>();
        //private List<User> Users = new List<User>();
        //        private Dictionary<int, List<Thread>> recvThreadList = new Dictionary<int, List<Thread>>();
        private Socket _socket;
        private static List<ChatUserInfo> userinfo = new List<ChatUserInfo>();
        SynchronizationContext _syncContext = null;
        private DispatcherTimer _getEquipment_timer = new DispatcherTimer();
        private GenerateXML hotaXML = new GenerateXML();
        public MainWindow()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
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
            tb_message.Text = "";
            int n;
            if (!int.TryParse(tb_port.Text, out n) || int.Parse(tb_port.Text) <= 1024)
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "Port 請輸入大於1024的數字。";
                tb_port.Focus();
                return;
            }
            if (cb_ipaddress.SelectedIndex < 0)
            {
                cb_ipaddress.Focus();
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "請選擇IP Address.";
                return;
            }

            string strConn = "Data Source=10.3.30.203;Database=hotaMeasure;User Id=sa;Password=hota;";
            string strmysqlConn = "server=localhost;uid=root;pwd=hota;database=hota";
            if (cb_database.SelectedIndex == 0)
            {
                try
                {
                    sqlConnection = new SqlConnection(strConn);
                    
                    ReaderInfo = SLManage.GetSetting(sqlConnection);
                    if (ReaderInfo.Count == 0)
                    {
                        lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                        lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
                        return;
                        //MessageBox.Show("連線DB失敗或無設定資料，請連絡資訊人員。");
                        //Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    PGMethod.WriteLog("連線DB失敗：" + ex.Message);
                    lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
                    return;
                    //MessageBox.Show("連線DB失敗，請連絡資訊人員。");
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
                        lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                        lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
                        return;
                        //MessageBox.Show("連線DB失敗或無設定資料，請連絡資訊人員。");
                        //Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    PGMethod.WriteLog("連線DB失敗：" + ex.Message);
                    //MessageBox.Show("連線DB失敗，請連絡資訊人員。");
                    lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
                    return;
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
            #region 舊程式
            /*
            CreateSocket();
            if (BindAndListen())
            {
                WaitClientConnection();
            }
            */
            #endregion
            BindAndListen();
        }
        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        #region 舊程式
        /*
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
        */
        #endregion
        private void BindAndListen()
        {
            #region 取得設備清單初始值
            _getEquipment_timer.Interval = TimeSpan.FromSeconds(5);
            _getEquipment_timer.Tick += _getEquipment_Tick;
            _getEquipment_timer.Start();
            #endregion
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endport = new IPEndPoint(IPAddress.Parse(cb_ipaddress.SelectedItem.ToString()), int.Parse(tb_port.Text));
            socket.Bind(endport);
            socket.Listen(10);
            Thread thread = new Thread(Recevice);
            thread.IsBackground = true;
            thread.Start(socket);
            lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 255));
            lb_status.Content = "伺服器開始服務中...";
            #region 舊程式
            /*
            hotaserver = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            hotaserver.Bind(new IPEndPoint(iPAddress, port));
            hotaserver.Listen(100);
            hotaserver.BeginAccept(newConnection, null);
            */
            #endregion
            //WaitClientConnection();
        }
        private void Recevice(object obj)
        {
            var socket = obj as Socket;
            while (true)
            {
                string remoteEpInfo = string.Empty;
                try
                {
                    Socket txSocket = socket.Accept();
                    _socket = txSocket;
                    if (txSocket.Connected)
                    {
                        remoteEpInfo = txSocket.RemoteEndPoint.ToString();
                        if (!findEquipment(remoteEpInfo))
                        {
                            txSocket.Shutdown(SocketShutdown.Receive);
                            continue;
                        }
                        _syncContext.Post(SetTextBoxText, remoteEpInfo + ":已上線..." + "\n");
                        
                        ChatUserInfo clientUser = new ChatUserInfo
                        {
                            UserID = Guid.NewGuid().ToString(),
                            ChatUid = remoteEpInfo,
                            ChatSocket = txSocket
                        };
                        userinfo.Add(clientUser);
                        _syncContext.Post(AddUserList, clientUser.ChatUid);
                        //clientMessage(" is check in", remoteEpInfo);
                        ReceseMsgGoing(txSocket, remoteEpInfo);
                    }
                    else
                    {
                        if (userinfo.Count > 0)
                        {
                            userinfo.Remove(userinfo.Where(c => c.ChatUid == remoteEpInfo).FirstOrDefault());
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    if (userinfo.Count > 0)
                    {
                        userinfo.Remove(userinfo.Where(c => c.ChatUid == remoteEpInfo).FirstOrDefault());
                    }
                }
            }
        }
        private void ReceseMsgGoing(Socket txSocket, string remoteEpInfo)
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        byte[] recesiveByte = new byte[RDataLen];
                        int getlength = txSocket.Receive(recesiveByte);
                        if (getlength <= 0) { break; }
                        string getmsg = Encoding.UTF8.GetString(recesiveByte, 0, getlength);
                        //_syncContext.Post(SetTextBoxText, remoteEpInfo + ":" + getmsg + "\n");
                        _syncContext.Post(SetTextBoxText, remoteEpInfo + ":已傳來訊息\n");
                        processData(remoteEpInfo, getmsg);
                        //clientMessage(": " + getmsg, remoteEpInfo);
                    }
                    catch (Exception ex)
                    {
                        _syncContext.Post(RemoveUserList, remoteEpInfo);
                        userinfo.Remove(userinfo.FirstOrDefault(c => c.ChatUid == remoteEpInfo));
                        sendLeaveClint(remoteEpInfo);
                        txSocket.Dispose();
                        txSocket.Close();
                        break;
                    }
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }
        private void SetTextBoxText(object text)
        {
            tb_message.Text = text.ToString() + tb_message.Text;
        }
        private void AddUserList(object userinfo)
        {
            lb_users.Items.Add(userinfo.ToString());
        }
        private void RemoveUserList(object removeinfo)
        {
            lb_users.Items.Remove(removeinfo.ToString());
        }
        #region 舊程式
        /*
        while (true)
        {
            //PGMethod.WriteLog("當前連結數量：" + recvThreadList.Count);
            Socket ClientSocket = hotaserver.Accept();
                
            if (ClientSocket != null)
            {
                foreach (SocketLocation SLRow in ReaderInfo)
                {
                    string remoteip = ClientSocket.RemoteEndPoint.ToString();
                    string[] words = remoteip.Split(':');
                    if (words[0] == SLRow.ip_address)
                    {
                        //PGMethod.WriteLog(ClientSocket.RemoteEndPoint + " 連線成功！");
                        //ConnectSockeList.Add(ClientSocket);
                        Thread recv;
                        Thread send;
                        int DBtype = cb_database.SelectedIndex;
                        switch (SLRow.location)
                        {
                            case "GG":
                                recv = new Thread(RecvGGMessage);
                                send = new Thread(SendMessage);
                                recvThreadList.Add(Index, new List<Thread> { recv, send });
                                recv.Start(new ArrayList { Index, ClientSocket, SLRow, DBtype });
                                send.Start(new ArrayList { Index, ClientSocket });
                                break;
                            case "IG":
                                recv = new Thread(RecvIGMessage);
                                send = new Thread(SendMessage);
                                recvThreadList.Add(Index, new List<Thread> { recv });
                                recv.Start(new ArrayList { Index, ClientSocket, SLRow, DBtype });
                                send.Start(new ArrayList { Index, ClientSocket });
                                break;
                            case "QC2":
                                recv = new Thread(RecvFQCMessage);
                                send = new Thread(SendMessage);
                                recvThreadList.Add(Index, new List<Thread> { recv, send });
                                recv.Start(new ArrayList { Index, ClientSocket });
                                send.Start(new ArrayList { Index, ClientSocket });
                                break;
                            case "QC1":
                                recv = new Thread(RecvFQCMessage);
                                send = new Thread(SendMessage);
                                recvThreadList.Add(Index, new List<Thread> { recv, send });
                                recv.Start(new ArrayList { Index, ClientSocket });
                                send.Start(new ArrayList { Index, ClientSocket });
                                break;
                        }
                    }
                }
                recvThreadList.Remove(Index);
                //ClientSocket.Shutdown(SocketShutdown.Both);
                //Index++;
            }
        }
        */
        #endregion

        #region 舊程式
        /*
        private void RecvGGMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
            SocketLocation SLData = arraylist[2] as SocketLocation;
            int DBtype = (int)arraylist[3];
            GGManage = new GearGrindingManage();
            while (true)
            {
                try
                {
                    byte[] strbyte = new byte[2048];
                    int count = clientsocket.Receive(strbyte);
                    string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                    GGData = new GearGrinding();
                    string remoteip = clientsocket.RemoteEndPoint.ToString();
                    string[] words = remoteip.Split(':');
                    if (words[0] == SLData.ip_address)
                    {
                        GGData.product_id = ret.Trim();
                        GGData.qctime = DateTime.Now;
                        GGData.reader_name = SLData.readerno;
                        GGData.reader_ip = SLData.ip_address;
                        GGData.point_name = SLData.pointname;
                            
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
        private void RecvIGMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
            SocketLocation SLData = arraylist[2] as SocketLocation;
            int DBtype = (int)arraylist[3];
            IGManage = new InnerDiameterManage();
            //PGMethod.WriteLog("執行接收資料");
            while (true)
            {
                try
                {
                    byte[] strbyte = new byte[2048];
                    int count = clientsocket.Receive(strbyte);
                    string ret = Encoding.UTF8.GetString(strbyte, 0, count);
                    string remoteip = clientsocket.RemoteEndPoint.ToString();
                    string[] words = remoteip.Split(':');
                    if (words[0] == SLData.ip_address)
                    {
                        string[] getMessage = ret.Split('\n');
                        string[] columnArr = getMessage[1].Split(',');
                        if (columnArr[0].Trim() == "Machine Name") continue;
                    
                        IGData = new innerdiameter();
                        IGData.product_id = columnArr[1].Trim();
                        IGData.qctime = Convert.ToDateTime(columnArr[9].Trim());
                        IGData.qc_result = columnArr[4].Trim();
                        IGData.q3measured = Convert.ToDecimal(columnArr[6].Trim());
                        IGData.machine_no = SLData.readerno;
                        if (DBtype == 0)
                        {
                            if (IGManage.CheckProductExist(IGData, sqlConnection))
                            {
                                IGManage.UpdateTable(IGData, sqlConnection);
                                PGMethod.WriteIGData(IGData);
                            }
                            else
                            {
                                IGManage.InsertTable(IGData, sqlConnection);
                                PGMethod.WriteIGData(IGData);
                            }
                        }
                        else
                        {
                            if (IGManage.CheckProductMysql(IGData, MysqlConnection))
                            {
                                IGManage.UpdateTableMysql(IGData, MysqlConnection);
                                PGMethod.WriteIGData(IGData);
                            }
                            else
                            {
                                IGManage.InsertTableMysql(IGData, MysqlConnection);
                                PGMethod.WriteIGData(IGData);
                            }
                        }
                    }
                }
                catch (Exception recv)
                {
                    PGMethod.WriteLog(recv.Message);
//                    recvThreadList[index][1].Abort();
//                    recvThreadList.Remove(index);
                }
            }
        }
        private void RecvFQCMessage(object client_socket)
        {
            ArrayList arraylist = client_socket as ArrayList;
            int index = (int)arraylist[0];
            Socket clientsocket = arraylist[1] as Socket;
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
                    PGMethod.WriteLog("send:" + send.Message);
//                    recvThreadList[index][1].Abort();
//                    recvThreadList.Remove(index);
                }
            }
        }
        */
        #endregion

        private void bt_sendmsg_Click(object sender, RoutedEventArgs e)
        {
            var getmSg = tb_sendmsg.Text.Trim();
            if (string.IsNullOrWhiteSpace(getmSg))
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "請輸入發送訊息。";
                return;
            }
            var obj = lb_users.SelectedItem;
            int getindex = lb_users.SelectedIndex;
            if (obj == null || getindex == -1)
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "請先指定發送用戶端。";
                return;
            }
            //var getChoseUser = obj as ChatUserInfoBase;
            var sendMsg = ServiceSockertHelper.GetSendMsgByte(getmSg, ChatTypeInfoEnum.StringEnum);
            userinfo.FirstOrDefault(c => c.ChatUid == obj.ToString())?.ChatSocket?.Send(sendMsg);
            //userinfo.FirstOrDefault(c => c.ChatUid == getChoseUser.ChatUid)?.ChatSocket?.Send(sendMsg);
        }

        private void bt_sendgroup_Click(object sender, RoutedEventArgs e)
        {
            var getmSg = tb_sendmsg.Text.Trim();
            if (string.IsNullOrWhiteSpace(getmSg))
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "請輸入發送訊息";
                return;
            }
            if (userinfo.Count <= 0)
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "暫時沒有用戶端登錄";
                return;
            }
            var sendMsg = ServiceSockertHelper.GetSendMsgByte(getmSg, ChatTypeInfoEnum.StringEnum);
            foreach (var usersocket in userinfo)
            {
                usersocket.ChatSocket?.Send(sendMsg);
            }
        }
        private void sendLeaveClint(string remoteEpInfo)
        {
            var sendMsg = ServiceSockertHelper.GetSendMsgByte(remoteEpInfo + " is leave.", ChatTypeInfoEnum.StringEnum);
            foreach (var usersocket in userinfo)
            {
                usersocket.ChatSocket?.Send(sendMsg);
            }
        }
        private void clientMessage(string remoteEpInfo, string msg)
        {
            //PGMethod.WriteLog(msg);
            //var sendMsg = ServiceSockertHelper.GetSendMsgByte(remoteEpInfo + ": " + msg, ChatTypeInfoEnum.StringEnum);
            ChatUserInfo client = userinfo.FirstOrDefault(c => c.ChatUid == remoteEpInfo);
            client.ChatSocket.Send(Encoding.UTF8.GetBytes(msg));
            /*
            foreach (var usersocket in userinfo)
            {
                if (usersocket == client) continue;
                usersocket.ChatSocket?.Send(sendMsg);
            }
            */
        }
        private void _getEquipment_Tick(object sender, EventArgs e)
        {
            if (cb_database.SelectedIndex == 0)
            {
                ReaderInfo = SLManage.GetSetting(sqlConnection);
            }
            else
            {
                ReaderInfo = SLManage.GetMysqlSetting(MysqlConnection);
            }
        }
        private bool findEquipment(string remoteEpInfo)
        {
            string[] remoteArr = remoteEpInfo.Split(':');
            foreach (SocketLocation location in ReaderInfo)
            {
                if (location.ip_address == remoteArr[0])
                {
                    foreach (ChatUserInfo user in userinfo)
                    {
                        string[] socketinfoArr = user.ChatUid.Split(':');
                        if (socketinfoArr[0] == remoteArr[0]) return false;
                    }
                    return true;
                }
            }
            return false;
        }
        private bool processData(string remoteEpInfo, string remoteData)
        {
            string[] ipaddressArr = remoteEpInfo.Split(':');
            foreach (SocketLocation location in ReaderInfo)
            {
                if (location.ip_address == ipaddressArr[0])
                {
                    switch (location.location)
                    {
                        case "QC2":
                            leonardoProcess(remoteEpInfo, remoteData);
                            break;
                    }
                    return true;
                }
            }
            return false;
        }
        private void leonardoProcess(string remoteEpInfo, string xmlData)
        {
            PGMethod.WriteLog(xmlData);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlData.Trim());
            XmlElement root = doc.DocumentElement;
            string corr_id = root.GetAttribute("corr_Id");
            string fileType = doc.GetElementsByTagName("trx_id")[0]?.InnerText;
            string returnmsg;
            switch (fileType)
            {
                case "ONLINE01":
                    returnmsg = hotaXML.CreateOnline02(corr_id);
                    PGMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    returnmsg = hotaXML.CreateTimeSyc01(corr_id);
                    PGMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "TMESYC02":
                    returnmsg = hotaXML.CreateCURInfo01(corr_id);
                    PGMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "CURINF02":
                    _syncContext.Post(SetTextBoxText, remoteEpInfo + ":正常執行中...\n");
                    break;
                case "WRKEND01":
                    returnmsg = hotaXML.CreateWorkEND02(corr_id);
                    PGMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                default:
                    PGMethod.WriteLog("無法判斷檔案類型");
                    break;
            }
        }
    }
}

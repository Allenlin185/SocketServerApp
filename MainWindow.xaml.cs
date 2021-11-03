using MySql.Data.MySqlClient;
using MySQLOperator;
using ProgramMethod;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using XMLMethod;
using DBModels;
namespace SocketServerApp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private MySqlConnection connect;
        private FileMethod fileMethod = new FileMethod();
        private DBMethod mysql = new DBMethod();
        private SocketLocationManage SLManage = new SocketLocationManage();
        private List<SocketLocation> ReaderInfo = new List<SocketLocation>();
        private int RDataLen = 5120;
        SynchronizationContext _syncContext = null;
        private DispatcherTimer _getEquipment_timer = new DispatcherTimer();
        private GenerateXML hotaXML = new GenerateXML();
        private byte[] KeepAlive()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)1000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            return inOptionValues;
        }
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
        private void Initial()
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
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                ShowInTaskbar = false;
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "程式說明", notifyIcon.Text, ToolTipIcon.Info);
            }
        }
        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
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

            connect = mysql.getConnect();
            if (connect == null)
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
            }
            ReaderInfo = SLManage.GetSetting(connect);
            if (ReaderInfo.Count == 0)
            {
                lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                lb_status.Content = "連線DB失敗或無設定資料，請連絡資訊人員。";
                return;
            }
            btn_start.Visibility = Visibility.Hidden;
            btn_stop.Visibility = Visibility.Visible;
            BindAndListen();
        }
        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void BindAndListen()
        {
            _getEquipment_timer.Interval = TimeSpan.FromSeconds(5);
            _getEquipment_timer.Tick += GetEquipment_Tick;
            _getEquipment_timer.Start();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endport = new IPEndPoint(IPAddress.Parse(cb_ipaddress.SelectedItem.ToString()), int.Parse(tb_port.Text));
            socket.Bind(endport);
            socket.Listen(50);
            socket.IOControl(IOControlCode.KeepAliveValues, KeepAlive(), null);
            Thread thread = new Thread(Recevice);
            thread.IsBackground = true;
            thread.Start(socket);
            lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 255));
            lb_status.Content = "伺服器開始服務中...";
            lv_eqlist.ItemsSource = ReaderInfo;
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
                    if (txSocket.Connected)
                    {
                        remoteEpInfo = txSocket.RemoteEndPoint.ToString();
                        if (!findEquipment(remoteEpInfo))
                        {
                            txSocket.Shutdown(SocketShutdown.Receive);
                            txSocket.Dispose();
                            txSocket.Close();
                            continue;
                        }
                        else
                        {
                            _syncContext.Post(ConnectSocketTrue, txSocket);
                            ReceseMsgGoing(txSocket, remoteEpInfo);
                        }
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    lb_status.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                    lb_status.Content = "偵測客戶端連線失敗，請連絡資訊人員。";
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
                        if (getmsg == "") continue;
                        processData(remoteEpInfo, getmsg);
                    }
                    catch (Exception Ex)
                    {
                        Console.WriteLine("out line: " + Ex.Message);
                        _syncContext.Post(ConnectSocketFalse, remoteEpInfo);
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
        private void RefreshEQList(object EQList)
        {
            lv_eqlist.ItemsSource = ReaderInfo;
            lv_eqlist.Items.Refresh();
        }
        private void ConnectSocketTrue(object connectSocket)
        {
            Socket clientSocket = (Socket)connectSocket;
            string remoteEpInfo = clientSocket.RemoteEndPoint.ToString();
            string[] remoteArr = remoteEpInfo.ToString().Split(':');
            SocketLocation client = ReaderInfo.FirstOrDefault(c => c.ip_address == remoteArr[0]);
            if (string.IsNullOrEmpty(client.wip))
            {
                client.statusImage = "Images\\online.png";
                client.SocketUid = remoteArr[1];
                client.dt_getdata = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lv_eqlist.Items.Refresh();
            }
            client.ConnectSocket = clientSocket;
        }
        private void ConnectSocketFalse(object removeinfo)
        {
            string remoteEpInfo = removeinfo.ToString();
            string[] remoteArr = remoteEpInfo.ToString().Split(':');
            SocketLocation client = ReaderInfo.FirstOrDefault(c => c.ip_address == remoteArr[0]);
            if (client != null)
            {
                client.statusImage = "Images\\offline.png";
                client.SocketUid = "";
                client.dt_getdata = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                client.ConnectSocket = null;
            }
            
            lv_eqlist.Items.Refresh();
        }
        private void AcceptData(object removeinfo)
        {
            SocketLocation DataInfo = (SocketLocation)removeinfo;
            SocketLocation client = ReaderInfo.FirstOrDefault(c => c.ip_address == DataInfo.ip_address);
            if (client != null)
            {
                client.dt_getdata = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                client.wip = DataInfo.wip;
                lv_eqlist.Items.Refresh();
            }
        }
        private void clientMessage(string removeinfo, string msg)
        {
            //PGMethod.WriteLog(msg);
            string remoteEpInfo = removeinfo.ToString();
            string[] remoteArr = remoteEpInfo.ToString().Split(':');
            SocketLocation client = ReaderInfo.FirstOrDefault(c => c.ip_address == remoteArr[0]);
            client.ConnectSocket.Send(Encoding.UTF8.GetBytes(msg));
        }
        private void GetEquipment_Tick(object sender, EventArgs e)
        {
            List<SocketLocation> newInfo = new List<SocketLocation>();
            newInfo = SLManage.GetSetting(connect);
            foreach (SocketLocation listrow in newInfo)
            {
                if (ReaderInfo.FindIndex(c => c.ip_address == listrow.ip_address) == -1)
                {
                    ReaderInfo.Add(listrow);
                }
            }
            for (int i = ReaderInfo.Count - 1; i >= 0; i--)
            {
                SocketLocation checkrow = ReaderInfo[i];
                if (newInfo.FindIndex(c => c.ip_address == ReaderInfo[i].ip_address) == -1)
                {
                    if (checkrow.ConnectSocket != null)
                    {
                        checkrow.ConnectSocket.Dispose();
                        checkrow.ConnectSocket.Close();
                    }
                    ReaderInfo.RemoveAt(i);
                }
            }
            _syncContext.Post(RefreshEQList, ReaderInfo);
        }
        private bool findEquipment(string remoteEpInfo)
        {
            string[] remoteArr = remoteEpInfo.Split(':');
            SocketLocation client = ReaderInfo.FirstOrDefault(c => c.ip_address == remoteArr[0]);
            if (client != null) return true;
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
                        case "GG":
                            RecvGGMessage(location, remoteData);
                            break;
                        case "IG":
                            RecvIGMessage(location, remoteData);
                            break;
                        case "YG":
                        case "YG-M":
                            RecvYGMessage(location, remoteData);
                            break;
                        case "QC2":
                            leonardoProcess(remoteEpInfo, remoteData);
                            break;
                    }
                    return true;
                }
            }
            return false;
        }
        private void RecvGGMessage(SocketLocation location, string ReaderData)
        {
            GearGrindingManage GGManage = new GearGrindingManage();
            try
            {
                GearGrinding GGData = new GearGrinding();
                GGData.product_id = ReaderData.Trim();
                GGData.qctime = DateTime.Now;
                GGData.reader_name = location.readerno;
                GGData.reader_ip = location.ip_address;
                GGData.point_name = location.pointname;
                location.wip = ReaderData.Trim();
                if (GGManage.CheckProduct(GGData, connect))
                {
                    GGManage.UpdateTable(GGData, connect);
                }
                else
                {
                    GGManage.InsertTable(GGData, connect);
                }
                fileMethod.WriteGGData(GGData);
                _syncContext.Post(AcceptData, location);
            }
            catch (Exception recv)
            {
                fileMethod.WriteLog(recv.Message);
            }
        }
        private void RecvIGMessage(SocketLocation location, string ReaderData)
        {
            InnerdiameterManage IGManage = new InnerdiameterManage();
            fileMethod.WriteLog(ReaderData);
            if (ReaderData == "$TIME")
            {
                string remoteEpInfo = location.ip_address + ":" + location.SocketUid;
                clientMessage(remoteEpInfo, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return;
            }
            
            try
            {
                string[] getMessage = ReaderData.Split('\n');
                string[] columnArr = getMessage[1].Split(',');
                ShareTable IGData = new ShareTable();
                if (string.IsNullOrEmpty(columnArr[1].Trim()))
                {
                    fileMethod.WriteLog("The Innerdiameter WIP is null.");
                }
                else
                {
                    DateTime qctime = DateTime.MinValue;
                    IGData.cID = columnArr[1].Trim();
                    IGData.cDateTime = Convert.ToDateTime(columnArr[9].Trim());
                    IGData.cResult = columnArr[4].Trim();
                    IGData.cComment = "";
                    location.wip = columnArr[1].Trim();
                    if (IGManage.CheckProductExist(IGData.cID, connect) > qctime)
                    {
                        IGManage.UpdateTable(IGData, connect);
                    }
                    else
                    {
                        IGManage.InsertTable(IGData, connect);
                    }
                    fileMethod.WriteIGDataforMerlin(getMessage, location.readerno);
                    _syncContext.Post(AcceptData, location);
                }

            }
            catch (Exception recv)
            {
                fileMethod.WriteLog(recv.Message);
            }
        }
        private void RecvYGMessage(SocketLocation location, string ReaderData)
        {
            ExternaldiameterManage YGManage = new ExternaldiameterManage();
            if (ReaderData == "$TIME")
            {
                string remoteEpInfo = location.ip_address + ":" + location.SocketUid;
                clientMessage(remoteEpInfo, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return;
            }
            
            try
            {
                string[] getMessage = ReaderData.Split('\n');
                string[] columnArr = getMessage[1].Split(',');
                ShareTable YGData = new ShareTable();
                if (string.IsNullOrEmpty(columnArr[1].Trim()))
                {
                    fileMethod.WriteLog("The Externaldiameter WIP is null.");
                }
                else
                {
                    DateTime qctime = DateTime.MinValue;
                    YGData.cID = columnArr[1].Trim();
                    YGData.cDateTime = Convert.ToDateTime(columnArr[9].Trim());
                    YGData.cResult = columnArr[4].Trim();
                    YGData.cComment = "";
                    location.wip = columnArr[1].Trim();
                    if (YGManage.CheckProductExist(YGData.cID, connect) > qctime)
                    {
                        YGManage.UpdateTable(YGData, connect);
                    }
                    else
                    {
                        YGManage.InsertTable(YGData, connect);
                    }
                    fileMethod.WriteIGDataforMerlin(getMessage, location.readerno);
                    _syncContext.Post(AcceptData, location);
                }
            }
            catch (Exception recv)
            {
                fileMethod.WriteLog(recv.Message);
            }
        }
        //  Leonardo 等待原廠回覆後，再執行
        private void leonardoProcess(string remoteEpInfo, string xmlData)
        {
            fileMethod.WriteLog(xmlData);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlData.Trim());
            XmlElement root = doc.DocumentElement;
            string corr_id = root.GetAttribute("corr_Id");
            string fileType = doc.GetElementsByTagName("trx_id")[0]?.InnerText;
            string returnmsg;
            switch (fileType)
            {
                case "ONLINE01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateOnline02(corr_id);
                    //fileMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateTimeSyc01(corr_id);
                    //fileMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "TMESYC02":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateCURInfo01(corr_id);
                    //fileMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "CURINF02":
                    break;
                case "EQPSTS01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateEQPSTS02(corr_id);
                    //fileMethod.WriteLog(returnmsg);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "WRKEND01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateWorkEND02(corr_id);
                    fileMethod.WriteLog(returnmsg);

                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                default:
                    fileMethod.WriteLog("無法判斷檔案類型");
                    break;
            }
        }
    }
}

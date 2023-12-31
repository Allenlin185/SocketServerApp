﻿using MySql.Data.MySqlClient;
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
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Forms.Application;

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
        private static Mutex mutex;
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mutex = new Mutex(true, "OnlyRun");
            if (mutex.WaitOne(0, false))
            {
                //Application.Run(new MainForm());
            }
            else
            {
                MessageBox.Show("程式已經在執行！", "提示");
                Environment.Exit(0);
            }

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
                        txSocket.Dispose();
                        txSocket.Close();
                        _syncContext.Post(ConnectSocketFalse, remoteEpInfo);
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
                client.status = 1;
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
                client.status = 0;
                client.SocketUid = "";
                client.wip = "";
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
                int rowIndex = newInfo.FindIndex(c => c.ip_address == ReaderInfo[i].ip_address);
                if (rowIndex == -1)
                {
                    if (checkrow.ConnectSocket != null)
                    {
                        checkrow.ConnectSocket.Dispose();
                        checkrow.ConnectSocket.Close();
                    }
                    ReaderInfo.RemoveAt(i);
                }
                else
                {
                    checkrow.ip_address = newInfo[rowIndex].ip_address;
                    checkrow.location = newInfo[rowIndex].location;
                    checkrow.readerno = newInfo[rowIndex].readerno;
                    checkrow.pointname = newInfo[rowIndex].pointname;
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
                        case "YG":
                        case "YG-M":
                            RecvYGMessage(location, remoteData);
                            break;
                        case "QC1":
                        case "QC2":
                        case "FQC":
                        case "IG":
                            leonardoProcess(remoteEpInfo, remoteData, location);
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
        /*
        private void RecvIGMessage(SocketLocation location, string ReaderData)
        {
            InnerdiameterManage IGManage = new InnerdiameterManage();
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
        */
        private void RecvYGMessage(SocketLocation location, string ReaderData)
        {
            if (ReaderData == "$TIME")
            {
                string remoteEpInfo = location.ip_address + ":" + location.SocketUid;
                clientMessage(remoteEpInfo, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                return;
            }
            ExternaldiameterManage YGManage = new ExternaldiameterManage();
            string[] getMessage = ReaderData.Split('\n');
            string[] columnArr;
            string ErroeLog = "The Externaldiameter File incomplete.";
            if (getMessage.Length < 7)
            {
                if (getMessage.Length > 1)
                {
                    columnArr = getMessage[1].Split(',');
                    if (columnArr.Length == 10)
                        ErroeLog += " : " + location.readerno + "[" + columnArr[9].Trim() + "]";
                }
                fileMethod.WriteLog(ErroeLog);
                return;
            }
            columnArr = getMessage[1].Split(',');
            if (columnArr.Length != 10)
            {
                fileMethod.WriteLog(ErroeLog);
                return;
            }
            externaldiameter YGData = new externaldiameter();
            YGData = YGManage.initData(YGData);
            if (string.IsNullOrEmpty(columnArr[1].Trim()))
            {
                fileMethod.WriteLog("The Externaldiameter WIP is null." + " : " + location.readerno + "[" + columnArr[9].Trim() + "]");
                return;
            }
            DateTime qctime = DateTime.MinValue;
            foreach (string Rowstr in getMessage)
            {
                if (string.IsNullOrEmpty(Rowstr)) continue;
                columnArr = Rowstr.Split(',');
                switch (columnArr[5])
                {
                    case "D5":
                        YGData.product_id = columnArr[1].Trim();
                        YGData.qctime = Convert.ToDateTime(columnArr[9].Trim());
                        YGData.length = Convert.ToDecimal(columnArr[6].Trim());
                        YGData.machine_no = location.readerno;
                        location.wip = columnArr[1].Trim();
                        break;
                    case "D1":
                        YGData.edmeasured2 = Convert.ToDecimal(columnArr[6].Trim());
                        break;
                    case "D2":
                        YGData.edmeasured3 = Convert.ToDecimal(columnArr[6].Trim());
                        break;
                    case "R1":
                        YGData.yaw1 = Convert.ToDecimal(columnArr[6].Trim());
                        break;
                    case "R2":
                        YGData.yaw2 = Convert.ToDecimal(columnArr[6].Trim());
                        break;
                    case "":
                        YGData.qc_result = columnArr[8].Trim() == "G" ? "OK" : "NG";
                        break;

                }
            }
            DateTime existQctime = YGManage.CheckProductExist(YGData.product_id, connect);
            if (existQctime > qctime)
            {
                if (existQctime < YGData.qctime)
                {
                    if (YGManage.InsertDuplTable(YGData.product_id, YGData.qctime, connect))
                    {
                        YGManage.UpdateTable(YGData, connect);
                    }
                }
                else if(existQctime > YGData.qctime)
                {
                    YGManage.NewDuplTable(YGData, connect);
                }
            }
            else
            {
                YGManage.InsertTable(YGData, connect);
            }
            fileMethod.WriteDataforMerlin(getMessage, location.readerno);
            _syncContext.Post(AcceptData, location);
            #region use share table
            /*
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
            */
            #endregion
        }
        private void leonardoProcess(string remoteEpInfo, string xmlData, SocketLocation location)
        {
            fileMethod.WriteLeonardoXMLData(xmlData, location.readerno);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlData.Trim());
            XmlNode rootNode = doc.DocumentElement;
            string corr_id = rootNode.Attributes["corr_Id"].Value;
            XmlNode FileTypeNode = rootNode.SelectSingleNode("/TrxSet/TITA/trx_id");
            if (FileTypeNode == null)
            {
                FileTypeNode = rootNode.SelectSingleNode("/TrxSet/TOTA/trx_id");
                if (FileTypeNode == null) return;
            }
            if (string.IsNullOrEmpty(FileTypeNode.InnerText.Trim())) return;
            string fileType = FileTypeNode.InnerText.Trim();
            string returnmsg;
            switch (fileType)
            {
                case "ONLINE01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateOnline02(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateTimeSyc01(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "TMESYC02":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateCURInfo01(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "CURINF02":
                    break;
                case "EQPSTS01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateEQPSTS02(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "WRKEND01":
                    XmlNode WIPNode = rootNode.SelectSingleNode("/TrxSet/TITA/work_id");
                    if (string.IsNullOrEmpty(WIPNode.InnerText.Trim()))
                    {
                        Thread.Sleep(500);
                        returnmsg = hotaXML.CreateWorkEND02(corr_id);
                        clientMessage(remoteEpInfo, returnmsg);
                        return;
                    }
                    if (location.location == "QC1")
                    {
                        location.wip = ProcessQC1Data(doc);
                    }
                    else if (location.location == "QC2")
                    {
                        location.wip = ProcessQC2Data(doc);
                    }
                    else if (location.location == "IG")
                    {
                        location.wip = ProcessQC2Data(doc);
                        location.wip = RecvLeoIGMessage(doc);
                    }
                    else
                    {
                        location.wip = ProcessFQCData(doc);
                    }
                    _syncContext.Post(AcceptData, location);
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateWorkEND02(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                case "WRKSTA01":
                    Thread.Sleep(500);
                    returnmsg = hotaXML.CreateWRKSTA02(corr_id);
                    clientMessage(remoteEpInfo, returnmsg);
                    break;
                default:
                    fileMethod.WriteLog("無法判斷檔案類型");
                    break;
            }
        }
        private string ProcessQC1Data(XmlDocument doc)
        {
            Leonardoqc1Manage QC1Manage = new Leonardoqc1Manage();
            Leonardoqc1 QC1Data = hotaXML.resolveQC1XML(doc);
            DateTime existQctime = QC1Manage.CheckProductExist(QC1Data.product_id, connect);
            if (existQctime == DateTime.MinValue)
            {
                QC1Manage.InsertTable(QC1Data, connect);
            }
            else
            {
                if (existQctime == QC1Data.qctime) return QC1Data.product_id;
                if (existQctime < QC1Data.qctime)
                {
                    if (QC1Manage.InsertDuplTable(QC1Data.product_id, QC1Data.qctime, connect))
                    {
                        QC1Manage.UpdateTable(QC1Data, connect);
                    }
                }
                else
                {
                    QC1Manage.NewDuplTable(QC1Data, connect);
                }
            }
            return QC1Data.product_id;
        }
        private string ProcessQC2Data(XmlDocument doc)
        {
            Leonardoqc2Manage QC2Manage = new Leonardoqc2Manage();
            Leonardoqc201 QC2Data = hotaXML.resolveQC2XML01(doc);
            DateTime existQctime = QC2Manage.CheckProductExist(QC2Data.product_id, connect);
            if (existQctime == DateTime.MinValue)
            {
                QC2Manage.InsertTable01(QC2Data, connect);
            }
            else
            {
                if (existQctime == QC2Data.qctime) return QC2Data.product_id;
                if (existQctime < QC2Data.qctime)
                {
                    if (QC2Manage.InsertDuplTable01(QC2Data.product_id, QC2Data.qctime, connect))
                    {
                        QC2Manage.UpdateTable01(QC2Data, connect);
                    }
                }
                else
                {
                    QC2Manage.NewDuplTable01(QC2Data, connect);
                }
            }
            return QC2Data.product_id;
        }
        private string ProcessFQCData(XmlDocument doc)
        {
            LeonardoFQCManage FQCManage = new LeonardoFQCManage();
            Leonardofqc FQCData = hotaXML.resolveFQCXML(doc);
            DateTime existQctime = FQCManage.CheckProductExist(FQCData.product_id, connect);
            if (existQctime == DateTime.MinValue)
            {
                FQCManage.InsertTable(FQCData, connect);
            }
            else
            {
                if (existQctime == FQCData.qctime) return FQCData.product_id;
                if (existQctime < FQCData.qctime)
                {
                    if (FQCManage.InsertDuplTable(FQCData.product_id, FQCData.qctime, connect))
                    {
                        FQCManage.UpdateTable(FQCData, connect);
                    }
                }
                else
                {
                    FQCManage.NewDuplTable(FQCData, connect);
                }
            }
            return FQCData.product_id;
        }
        private string RecvLeoIGMessage(XmlDocument doc)
        {
            InnerdiameterManage IGManage = new InnerdiameterManage();
            innerdiameter01 IGData = new innerdiameter01();
            Leonardoqc201 QC2Data = hotaXML.resolveQC2XML01(doc);
            IGData.product_id = QC2Data.product_id;
            IGData.qctime = QC2Data.qctime;
            if (QC2Data.q1ro_result == "GOOD" && QC2Data.q2ro_result == "GOOD" 
                && QC2Data.q3diameter_result == "GOOD" && QC2Data.q4iro_result == "GOOD")
            {
                if (QC2Data.q7edr_result == "REJECT" || QC2Data.q8edr_result == "REJECT")
                {
                    IGData.qc_result = "REJECT";
                } 
                else
                {
                    IGData.qc_result = "GOOD";
                }
                
            }
            else
            {
                IGData.qc_result = "REJECT";
            }
            IGData.q1result = QC2Data.q1ro_result;
            IGData.q1measured = QC2Data.q1ro_measured;
            IGData.q1maxvalue = QC2Data.q1ro_maxvalue;
            IGData.q2result = QC2Data.q2ro_result;
            IGData.q2measured = QC2Data.q2ro_measured;
            IGData.q2maxvalue = QC2Data.q2ro_maxvalue;
            IGData.q3result = QC2Data.q3diameter_result;
            IGData.q3measured = QC2Data.q3diameter_measured;
            IGData.q3thresmin = QC2Data.q3diameter_thresmin;
            IGData.q3thresmax = QC2Data.q3diameter_thresmax;
            IGData.q4result = QC2Data.q4iro_result;
            IGData.q4measured = QC2Data.q4iro_measured;
            IGData.q4maxvalue = QC2Data.q4iro_maxvalue;
            IGData.q5result = QC2Data.q7edr_result;
            IGData.q5measured = QC2Data.q7edr_measured;
            IGData.q6result = QC2Data.q8edr_result;
            IGData.q6measured = QC2Data.q8edr_measured;
            IGData.machine_no = QC2Data.machine_no;
            IGData.ld_operator = QC2Data.ld_operator;
            DateTime existQctime = IGManage.CheckProductExist(IGData.product_id, connect);
            if (existQctime == DateTime.MinValue)
            {
                IGManage.InsertTable01(IGData, connect);
            }
            else
            {
                if (existQctime == IGData.qctime) return IGData.product_id;
                if (existQctime < IGData.qctime)
                {
                    if (IGManage.InsertDuplTable01(IGData.product_id, IGData.qctime, connect))
                    {
                        IGManage.UpdateTable01(IGData, connect);
                    }
                }
                else
                {
                    IGManage.NewDuplTable01(IGData, connect);
                }
            }
            return IGData.product_id;
        }
    }
}

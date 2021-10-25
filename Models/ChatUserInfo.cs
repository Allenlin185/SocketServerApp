using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerApp.Models
{
    using System.Net.Sockets;
    public class ChatUserInfoBase
    {
        public string UserID { get; set; }
        public string ChatUid { get; set; }
    }
    public class ChatUserInfo : ChatUserInfoBase
    {
        public Socket ChatSocket { get; set; }
    }
}

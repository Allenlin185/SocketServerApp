using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServerApp.Models
{
    using System.Net.Sockets;
    class ServiceSockertHelper
    {
        public static byte[] GetSendMsgByte(string msg, ChatTypeInfoEnum _enumType)
        {
            byte[] byMsg = Encoding.UTF8.GetBytes(msg);
            List<byte> byMsgAndType = new List<byte>();
            byMsgAndType.Add((byte)_enumType);
            byMsgAndType.AddRange(byMsg);
            return byMsgAndType.ToArray();
        }
    }
}

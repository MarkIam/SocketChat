using System.Net;
using System.Net.Sockets;

namespace SocketServer
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }
}

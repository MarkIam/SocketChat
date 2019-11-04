using System.Net;
using System.Net.Sockets;

namespace SocketServer.Classes
{
    public struct UdpState
    {
        public UdpClient client;
        public IPEndPoint endPoint;
    }
}

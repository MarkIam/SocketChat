using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BroadcastServer
{
    class Program
    {
        private static int port = 8888;

        static void Main(string[] args)
        {
            var ipAddress = GetLocalIPAddress();

            var Server = new UdpClient(port);
            var ResponseData = Encoding.ASCII.GetBytes($"{ipAddress}:{port}");

            Console.WriteLine("Server is waiting for connections...");

            while (true)
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 0);
                var ClientRequestData = Server.Receive(ref ClientEp);
                var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);

                Console.WriteLine("Received {0} from {1}, sending response", ClientRequest, ClientEp.Address.ToString());
                Server.Send(ResponseData, ResponseData.Length, ClientEp);
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
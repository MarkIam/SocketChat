using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer
{
    class Program
    {
        private static UdpClient Server;

        static int UdpserverPort; // порт для приема широковещательных запросов
        static int TcpServerPort; // порт для приема входящих TCP-запросов
        static byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
        static Socket listenSocket;
        
        static void Main(string[] args)
        {
            UdpserverPort = Convert.ToInt32(ConfigurationManager.AppSettings["UdpServerPort"]);
            TcpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["TcpServerPort"]);

            // запускаем сервер для ответа широковещательные запросы
            #region UDP Server launch
            Server = new UdpClient(UdpserverPort);

            Console.WriteLine("Server is waiting for broadcast connections...");

            var ClientEp = new IPEndPoint(IPAddress.Any, 0);
            UdpState udpState = new UdpState
            {
                e = ClientEp,
                u = Server
            };

            var ClientRequestData = Server.BeginReceive(UdpReceiveCallback, udpState);
            #endregion

            // получаем адреса для запуска сокета
            var ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), TcpServerPort);

            // создаем сокет
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipPoint);

                // начинаем прослушивание
                listenSocket.Listen(1);
                listenSocket.BeginAccept(AcceptCallback, listenSocket);

                Console.WriteLine("Server launched. Waitng for connections...");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            var srvSocket = (Socket)asyncResult.AsyncState;
            Socket clientSocket = srvSocket.EndAccept(asyncResult);
            clientSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);
            listenSocket.BeginAccept(AcceptCallback, listenSocket);
        }

        // обработчик запросов по сокету
        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            int received = socket.EndReceive(asyncResult);
            byte[] tempBuffer = new byte[received];
            Array.Copy(dataBuffer, tempBuffer, received);
            string messageReceived = Encoding.Unicode.GetString(tempBuffer);
            Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + messageReceived);

            var messageParts = messageReceived.Split(' ');

            //bool needToCloseSocket = false;
            string responseMessage = string.Empty;
            string userFrom = string.Empty;
            byte[] dataToSend;

            switch (messageParts[0])
            {
                // получить список пользователей
                case "listusers":
                    userFrom = messageParts[1];
                    if (!clients.ContainsKey(userFrom))
                    {
                        responseMessage = $"You are not registered.";
                        //needToCloseSocket = true;
                    }
                    else
                        responseMessage = clients.Keys.ToList().Aggregate((a, b) => { return $"{a}, {b}"; });

                    socket.Send(Encoding.Unicode.GetBytes(responseMessage));

                    break;
                // выйти
                case "exit":
                    userFrom = messageParts[1];
                    Socket socketToClose = null;
                    if (!clients.ContainsKey(userFrom))
                    {
                        responseMessage = $"You are not registered.";
                        //needToCloseSocket = true;
                    }
                    else
                    {
                        socketToClose = clients[userFrom];
                        clients.Remove(userFrom);
                        responseMessage = "Goodbye!";
                        //needToCloseSocket = true;
                    }
                    socket.Send(Encoding.Unicode.GetBytes(responseMessage));
                    if (socketToClose != null)
                    {
                        socketToClose.Shutdown(SocketShutdown.Both);
                        socketToClose.Close();
                    }
                    break;
                // зарегистрироваться
                case "register":
                    userFrom = messageParts[1];
                    if (clients.ContainsKey(userFrom))
                    {
                        responseMessage = $"'{userFrom}' is already registered. Please choose another name.";
                        //needToCloseSocket = true;
                    }
                    else
                    {
                        responseMessage = "You are successfully registered.";
                        clients.Add(userFrom, socket);
                    }

                    dataToSend = Encoding.Unicode.GetBytes(responseMessage);
                    socket.Send(dataToSend);

                    break;
                // отправить сообщение
                case "message":
                    userFrom = messageParts[3];
                    var userTo = messageParts[1];
                    if (!clients.ContainsKey(userFrom))
                    {
                        responseMessage = $"You are not registered.";
                        socket.Send(Encoding.Unicode.GetBytes(responseMessage));
                        //needToCloseSocket = true;
                    }
                    else if (!clients.ContainsKey(userTo))
                    {
                        responseMessage = $"Receiver '{userTo}' is not registered.";
                        socket.Send(Encoding.Unicode.GetBytes(responseMessage));
                        //needToCloseSocket = true;
                    }
                    else
                    {
                        var receiverSocket = clients[userTo];
                        dataToSend = Encoding.Unicode.GetBytes($"User '{userFrom}' sent you message: '{messageParts[2]}'");
                        receiverSocket.Send(dataToSend);

                        responseMessage = "Your message is successfully sent.";
                    }
                    break;
            }

            if (socket.Connected)
                socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
        }

        // получение локального IP-адреса
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        // обработчик широковещательного запроса 
        private static void UdpReceiveCallback(IAsyncResult asyncResult)
        {
            var state = (UdpState)(asyncResult.AsyncState);

            UdpClient u = state.u;
            IPEndPoint e = state.e;

            byte[] receiveBytes = u.EndReceive(asyncResult, ref e);
            var ClientRequest = Encoding.ASCII.GetString(receiveBytes);

            var ResponseData = Encoding.ASCII.GetBytes($"{GetLocalIPAddress()}:{TcpServerPort}");
            Console.WriteLine("Received {0} from {1}, sending response", ClientRequest, e.Address.ToString());
            Server.Send(ResponseData, ResponseData.Length, e);

            var ClientRequestData = Server.BeginReceive(UdpReceiveCallback, state);
        }
    }
}

using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketClient
{
    class Program
    {
        // адрес и порт сервера, к которому будем подключаться
        static string serverAddress = "127.0.0.1"; // адрес сервера
        static int serverPort; // порт сервера
        static string userName; // имя пользователя
        static byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static Socket socket;

        static void Main(string[] args)
        {
            try
            {
                var isServerKnown = ConfigurationManager.AppSettings["isServerAddressKnown"] == "1";

                if (!isServerKnown)
                {
                    var Client = new UdpClient();
                    var RequestData = Encoding.ASCII.GetBytes("GetServerChatAddress");
                    var ServerEp = new IPEndPoint(IPAddress.Any, 0);

                    Client.EnableBroadcast = true;
                    Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, 8005));

                    var ServerResponseData = Client.Receive(ref ServerEp);
                    var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);

                    Console.WriteLine("Received {0} from {1}", ServerResponse, ServerEp.Address.ToString());
                    var responseParts = ServerResponse.Split(':');

                    serverAddress = responseParts[0];
                    serverPort = Convert.ToInt32(responseParts[1]);

                    Client.Close();
                }
                else {
                    serverAddress = ConfigurationManager.AppSettings["ServerIpAddress"];
                    serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
                }

                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                socket.BeginConnect(ipPoint, ConnectedCallback, null);

                bool exit = false;
                while (!exit)
                {
                    Console.Write("Type your message:");
                    string messageToSend = Console.ReadLine();

                    bool needToSend = true;
                    var messageParts = messageToSend.Split(' ');
                    var command = messageParts[0].ToLowerInvariant();
                    switch (command)
                    {
                        case "listusers":
                        case "message":
                        case "exit":
                            if (string.IsNullOrEmpty(userName))
                            {
                                needToSend = false;
                                Console.WriteLine("You are not registered by the server.");
                            }
                            else
                            {
                                messageToSend = $"{messageToSend} {userName}";
                                exit = command == "exit";
                            }
                            break;
                        case "register":
                            userName = messageParts[1];
                            break;
                        case "help":
                            needToSend = false;
                            WriteHelp();
                            break;
                        default:
                            needToSend = false;
                            Console.WriteLine("Bad command");
                            WriteHelp();
                            break;
                    }

                    if (!needToSend)
                        continue;

                    byte[] data = Encoding.Unicode.GetBytes(messageToSend);

                    socket.Send(data);
                }
                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.Read();
        }

        private static void WriteHelp() {
            Console.WriteLine("Possible commands are:");
            Console.WriteLine("\tlistusers");
            Console.WriteLine("\tregister <userName>");
            Console.WriteLine("\tmessage <receiverName> <messageText>");
            Console.WriteLine("\texit");
        }

        // асинхронный обработчик подключения
        private static void ConnectedCallback(IAsyncResult asyncResult)
        {
            if (socket.Connected)
            {
                socket.EndConnect(asyncResult);
                Console.WriteLine("Connected to the server");
                socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
            }
            else
                Console.WriteLine("Couldn't connect");
        }

        // асинхронный обработчик приема сообщения
        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            if (!socket.Connected) return;
            int received = socket.EndReceive(asyncResult);
            byte[] tempBuffer = new byte[received];
            Array.Copy(dataBuffer, tempBuffer, received);
            string messageReceived = Encoding.Unicode.GetString(tempBuffer);

            Console.WriteLine($"You received message: {messageReceived}");

            socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        // адрес и порт сервера, к которому будем подключаться
        static int port = 8006; // порт сервера
        static string address = "127.0.0.1"; // адрес сервера
        static string userName; // имя пользователя
        static byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static Socket socket ;

        static void Main(string[] args)
        {
            try
            {
                // реализовать асинхронный прием сообщений, чтобы было возможно принимать сообщения от пользователей

                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                //socket.Connect(ipPoint);

                socket.BeginConnect(ipPoint, ConnectedCallback, null);

                //socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

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
                            //if (string.IsNullOrEmpty(userName))
                            //{
                            //    needToSend = false;
                            //    Console.WriteLine("You are not registered by the server.");
                            //}
                            //else
                            //{
                            messageToSend = $"{messageToSend} {userName}";
                            exit = command == "exit";
                            //}
                            break;
                        case "register":
                            userName = messageParts[1];
                            break;
                        case "help":
                            needToSend = false;
                            Console.WriteLine("Possible commands are:");
                            Console.WriteLine("\tlistusers");
                            Console.WriteLine("\tregister <userName>");
                            Console.WriteLine("\tmessage <receiverName> <messageText>");
                            Console.WriteLine("\texit");
                            break;
                        default:
                            needToSend = false;
                            Console.WriteLine("Bad commands");
                            break;
                    }

                    if (!needToSend)
                        continue;

                    byte[] data = Encoding.Unicode.GetBytes(messageToSend);

                    socket.Send(data);
                }
                //// закрываем сокет
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

        private static void ConnectedCallback(IAsyncResult asyncResult)
        {
            if (socket.Connected)
            {
                socket.EndConnect(asyncResult);
                Console.WriteLine("Connected to the server");
                socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            else
                Console.WriteLine("Couldn't connect");
        }

        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            int received = socket.EndReceive(asyncResult);
            byte[] tempBuffer = new byte[received];
            Array.Copy(dataBuffer, tempBuffer, received);
            string messageReceived = Encoding.Unicode.GetString(tempBuffer);

            Console.WriteLine($"You received message: {messageReceived}");

            socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer
{
    class Program
    {
        static byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static int port = 8006; // порт для приема входящих запросов
        static Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
        static Socket listenSocket;

        static void Main(string[] args)
        {
            // получаем адреса для запуска сокета
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            // создаем сокет
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipPoint);

                // начинаем прослушивание
                listenSocket.Listen(100);
                listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenSocket);

                Console.WriteLine("Server launched. Waitng for connections...");

                #region synchronous work
                //while (true)
                //{
                //    Socket handler = listenSocket.Accept();
                //    // получаем сообщение
                //    StringBuilder builder = new StringBuilder();
                //    int bytes = 0; // количество полученных байтов

                //    do
                //    {
                //        bytes = handler.Receive(data);
                //        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                //    }
                //    while (handler.Available > 0);

                //    var messageReceived = builder.ToString();


                //  если необходимо, закрываем сокет
                //  if (needToCloseSocket)
                //  {
                //      handler.Shutdown(SocketShutdown.Both);
                //        handler.Close();
                //  }
                //}
                #endregion
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
            clientSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
            listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenSocket);

            //byte[] Buffer;
            //int bytesTransferred;
            //var handler = srvSocket.EndAccept(out Buffer, out bytesTransferred, asyncResult);

            //string stringTransferred = Encoding.ASCII.GetString(Buffer, 0, bytesTransferred);
            //Console.WriteLine(stringTransferred);

            //handler.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), handler);
        }

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
                socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }
    }
}

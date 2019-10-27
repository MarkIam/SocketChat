using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using _utilities = Utilities.Utilities;

namespace SocketServer
{
    class Program
    {
        private static UdpClient _server;

        private static string _serverIpAddress; // IP-адрес сервера
        private static int _udpServerPort; // порт для приема широковещательных запросов
        private static int _tcpServerPort; // порт для приема входящих TCP-запросов

        private static byte[] _dataBuffer = new byte[256]; // буфер для получаемых данных
        // справочник клиентских сокетов по именам пользователей
        static Dictionary<string, Socket> _dictSocketsByUserName = new Dictionary<string, Socket>();
        static Socket listenSocket;

        const string MsgNotRegistered = "Вы не зарегистрированы.";

        static void Main(string[] args)
        {
            Console.Title = "Chat server";
            _utilities.SetConsoleEncoding();

            _udpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["UdpServerPort"]);
            _tcpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["TcpServerPort"]);

            try
            {
                _serverIpAddress = GetLocalIpAddress();
            }
            catch
            {
                _utilities.WriteMessageToConsole("Работа сервера возможна только в рамках одной рабочей станции.", true, EventLevel.Warning);
            }

            // запускаем сервер для ответа на широковещательные запросы
            #region UDP Server launch
            try
            {
                _server = new UdpClient(_udpServerPort);

                var clientEp = new IPEndPoint(IPAddress.Any, 0);
                var udpState = new UdpState
                {
                    e = clientEp,
                    u = _server
                };

                _server.BeginReceive(UdpReceiveCallback, udpState);
                _utilities.WriteMessageToConsole("Запущен сокет для широковещательных запросов на поиск чат-сервера.");
            }
            catch (Exception ex)
            {
                _utilities.WriteMessageToConsole($"При запуске сокета для широковещательных запросов произошла ошибка: {ex.Message}", true, EventLevel.Error);
                return;
            }
            #endregion

            // запускаем TCP-сокет
            #region TCP Chat Server launch
            var ipPoint = new IPEndPoint(IPAddress.Parse(_serverIpAddress), _tcpServerPort);
            //var ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _tcpServerPort);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);
                listenSocket.BeginAccept(AcceptCallback, listenSocket);

                _utilities.WriteMessageToConsole("Чат-сервер запущен и ожидает подключений...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                _utilities.WriteMessageToConsole($"При запуске чат-сервера произошла ошибка: {ex.Message}", true, EventLevel.Error);
            }
            #endregion
        }

        // обработчик приема соединения
        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            var srvSocket = (Socket)asyncResult.AsyncState;
            var clientSocket = srvSocket.EndAccept(asyncResult);
            clientSocket.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);

            srvSocket.BeginAccept(AcceptCallback, srvSocket);
        }

        // обработчик запросов по сокету
        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            var received = socket.EndReceive(asyncResult, out var se);
            if (se != SocketError.Success)
            {
                _utilities.WriteMessageToConsole("При получении сообщения от клиентов произошла ошибка.");
                return;
            }

            var tempBuffer = new byte[received];
            Array.Copy(_dataBuffer, tempBuffer, received);
            var messageReceived = _utilities.GetStringFromBytesReceived(tempBuffer);

            _utilities.WriteMessageToConsole($"Получено сообщение: {messageReceived}");

            var messageParts = messageReceived.Split(' ');
            string responseMessage, userFrom;
            var needToCloseSocket = false;

            switch (messageParts[0])
            {
                // получить список пользователей
                case "listusers":
                    userFrom = messageParts[1];
                    responseMessage = !IsUserRegistered(userFrom) ? MsgNotRegistered : $"Подключенные пользователи: {GetRegisteredUsers()}";

                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
                // выйти
                case "exit":
                    userFrom = messageParts[1];
                    if (!IsUserRegistered(userFrom))
                        responseMessage = MsgNotRegistered;
                    else
                    {
                        needToCloseSocket = true;
                        _dictSocketsByUserName.Remove(userFrom);
                        responseMessage = "До свидания!";
                    }
                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
                // зарегистрироваться
                case "register":
                    userFrom = messageParts[1];
                    if (IsUserRegistered(userFrom))
                        responseMessage = $"Пользователь '{userFrom}' уже зарегистрирован. Пожалуйста выберите другое имя.";
                    else
                    {
                        responseMessage = "Вы успешно зарегистрированы.";
                        _dictSocketsByUserName.Add(userFrom, socket);
                    }

                    socket.Send(_utilities.GetBytesToSend(responseMessage));

                    break;
                // отправить сообщение
                case "message":
                    userFrom = messageParts[3];
                    var userTo = messageParts[1];
                    if (!IsUserRegistered(userFrom))
                    {
                        responseMessage = MsgNotRegistered;
                    }
                    else if (!IsUserRegistered(userTo))
                    {
                        responseMessage = $"Получатель '{userTo}' не зарегистрирован на сервер.";
                    }
                    else
                    {
                        var receiverSocket = _dictSocketsByUserName[userTo];
                        receiverSocket.Send(_utilities.GetBytesToSend($"Пользователь '{userFrom}' прислал вам сообщение: '{messageParts[2]}'"));

                        responseMessage = "Ваше сообщение успешно отправлено.";
                    }
                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
            }

            if (needToCloseSocket)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            if (socket.Connected && !needToCloseSocket)
                socket.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
        }

        // обработчик широковещательного запроса 
        private static void UdpReceiveCallback(IAsyncResult asyncResult)
        {
            var state = (UdpState)(asyncResult.AsyncState);

            var u = state.u;
            var e = state.e;

            var receiveBytes = u.EndReceive(asyncResult, ref e);
            var clientRequest = _utilities.GetStringFromBytesReceived(receiveBytes);

            _utilities.WriteMessageToConsole($"Получен запрос '{clientRequest}' от {e.Address.ToString()}, отправляем ответ.");

            var responseData = _utilities.GetBytesToSend($"{_serverIpAddress}:{_tcpServerPort}");
            _server.Send(responseData, responseData.Length, e);

            _server.BeginReceive(UdpReceiveCallback, state);
        }

        #region Helpers

        // получение локального IP-адреса
        private static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();

            throw new Exception("Сетевые адаптеры не обнаружены.");
        }

        // проверка наличия пользователя в списке зарегистрированных
        private static bool IsUserRegistered(string userName)
        {
            return _dictSocketsByUserName.ContainsKey(userName);
        }

        // Получение списка зарегмстрированных пользователей
        private static string GetRegisteredUsers()
        {
            return _dictSocketsByUserName.Keys.ToList().Aggregate((a, b) => $"{a}, {b}");
        }
        #endregion
    }
}

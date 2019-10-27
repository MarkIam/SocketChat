using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using _utilities = Utilities.Utilities;
using EventLevel = System.Diagnostics.Tracing.EventLevel;

namespace SocketClient
{
    class Program
    {
        // адрес и порт сервера, к которому будем подключаться
        static string _serverAddress; // адрес сервера
        static int _serverPort; // порт сервера
        static string _userName; // имя пользователя
        static readonly byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static Socket _socket;

        static void Main(string[] args)
        {
            Console.Title = "Chat client";
            _utilities.SetConsoleEncoding();

            try
            {
                var isServerKnown = ConfigurationManager.AppSettings["isServerAddressKnown"] == "1";

                if (!isServerKnown)
                {
                    var client = new UdpClient();
                    var requestData = _utilities.GetBytesToSend("GetServerChatAddress");
                    var serverEp = new IPEndPoint(IPAddress.Any, 0);

                    client.EnableBroadcast = true;
                    client.Send(requestData, requestData.Length, new IPEndPoint(IPAddress.Broadcast, 8005));

                    var serverResponseData = client.Receive(ref serverEp);
                    var serverResponse = _utilities.GetStringFromBytesReceived(serverResponseData);

                    _utilities.WriteMessageToConsole($"От {serverEp.Address} получен ответ '{serverResponse}'");
                    var responseParts = serverResponse.Split(':');

                    _serverAddress = responseParts[0];
                    _serverPort = Convert.ToInt32(responseParts[1]);

                    client.Close();
                }
                else
                {
                    _serverAddress = ConfigurationManager.AppSettings["ServerIpAddress"];
                    _serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
                }

                var ipPoint = new IPEndPoint(IPAddress.Parse(_serverAddress), _serverPort);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // подключаемся к серверу
                try
                {
                    _socket.Connect(ipPoint);
                }
                catch (Exception e)
                {
                    _utilities.WriteMessageToConsole($"При подключении к чат-серверу произошла ошибка: {e.Message}");
                    Console.Read();
                }

                if (_socket.Connected)
                    _utilities.WriteMessageToConsole("Успешное подключение к серверу");
                else
                    _utilities.WriteMessageToConsole("Не удалось установить соединение с сервером.", true, EventLevel.Error);

                _socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);

                var exit = false;
                while (!exit)
                {
                    var needToSend = true;
                    _utilities.WriteMessageToConsole("Введите команду:");
                    var messageToSend = Console.ReadLine();

                    if (!ValidateMessage(messageToSend, out var validationMessage))
                    {
                        _utilities.WriteMessageToConsole(validationMessage, false, EventLevel.Error);
                        WriteHelp();
                        continue;
                    }

                    var messageParts = messageToSend.Split(' ');
                    var command = messageParts[0].ToLowerInvariant();

                    switch (command)
                    {
                        case "listusers":
                        case "message":
                        case "exit":
                            messageToSend = $"{messageToSend} {_userName}";
                            exit = command == "exit";
                            break;
                        case "register":
                            _userName = messageParts[1];
                            Console.Title = $"Chat client ({_userName})";
                            break;
                        case "help":
                            needToSend = false;
                            WriteHelp();
                            break;
                    }

                    if (!needToSend)
                        continue;

                    var data = _utilities.GetBytesToSend(messageToSend);
                    _socket.Send(data);
                }
                // закрываем сокет
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (Exception ex)
            {
                _utilities.WriteMessageToConsole($"Произошла ошибка: {ex.Message}", true, EventLevel.Error);
            }
        }

        // валидация введенной команды
        private static bool ValidateMessage(string messageToSend, out string validationMessage)
        {
            validationMessage = string.Empty;
            var validationResult = true;
            var messageParts = messageToSend.Split(' ');
            var command = messageParts[0].ToLowerInvariant();
            switch (command)
            {
                case "exit":
                case "help":
                    if (messageParts.Length != 1)
                    {
                        validationMessage = $"Передано ошибочное число параметров для команды {command}";
                        validationResult = false;
                    }
                    break;
                case "listusers":
                    if (messageParts.Length != 1)
                    {
                        validationMessage = $"Передано ошибочное число параметров для команды {command}";
                        validationResult = false;
                    }
                    else if (string.IsNullOrEmpty(_userName))
                    {
                        validationMessage = "Вы не зарегистрированы на сервере.";
                        validationResult = false;
                    }
                    break;
                case "message":
                    if (messageParts.Length != 3)
                    {
                        validationMessage = $"Передано ошибочное число параметров для команды {command}";
                        validationResult = false;
                    }
                    else if (messageParts[1].ToLowerInvariant() == _userName)
                    {
                        validationMessage = $"Сообщение не может быть отправлено самому себе.";
                        validationResult = false;
                    }
                    else if (string.IsNullOrEmpty(_userName))
                    {
                        validationMessage = "Вы не зарегистрированы на сервере.";
                        validationResult = false;
                    }

                    break;
                case "register":
                    if (messageParts.Length != 2)
                    {
                        validationMessage = $"Передано ошибочное число параметров для команды {command}";
                        validationResult = false;
                    }
                    else if (!string.IsNullOrEmpty(_userName))
                    {
                        validationMessage = $"Вы уже зарегистрированы.";
                        validationResult = false;
                    }
                    break;
                default:
                    validationMessage = "Неизвестная команда.";
                    validationResult = false;
                    break;
            }
            return validationResult;
        }

        // вывод справки
        private static void WriteHelp()
        {
            _utilities.WriteMessageToConsole("Possible commands are:", false);
            _utilities.WriteMessageToConsole("\tlistusers", false);
            _utilities.WriteMessageToConsole("\tregister <userName>", false);
            _utilities.WriteMessageToConsole("\tmessage <receiverName> <messageText>", false);
            _utilities.WriteMessageToConsole("\texit", false);
            _utilities.WriteMessageToConsole("\thelp", false);
        }

        // асинхронный обработчик приема сообщения
        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            if (!socket.Connected) return;

            var received = socket.EndReceive(asyncResult, out var se);
            if (se != SocketError.Success)
            {
                _utilities.WriteMessageToConsole($"При получении сообщения от чат-сервера произошла ошибка: {se}.");
                return;
            }

            var tempBuffer = new byte[received];
            Array.Copy(dataBuffer, tempBuffer, received);
            var messageReceived = _utilities.GetStringFromBytesReceived(tempBuffer);

            _utilities.WriteMessageToConsole($"Получено сообщение: {messageReceived}");

            socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
        }
    }
}
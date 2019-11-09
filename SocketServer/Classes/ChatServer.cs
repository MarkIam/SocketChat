using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using Utilities;
using _utilities = Utilities.Utilities;

namespace SocketServer.Classes
{
    public class ChatServer
    {
        private UdpClient udpServer; // сервер для ответа на широковещательные запросы

        private string serverIpAddress; // IP-адрес сервера
        private int udpServerPort; // порт для приема широковещательных запросов
        private int tcpServerPort; // порт для приема входящих TCP-запросов
        private int backlogSize; // размер беклога для сервера

        private byte[] dataBuffer = new byte[256]; // буфер для получаемых данных

        private RegisteredUsers registeredUsers; // класс зарегистрированных пользователей

        private Socket listenSocket; // сокет для приема команд пользователей чата
        private bool needToCloseClientConnection; // признак необходимости закрытия текущего соединения
        public Exception lastError { get; private set; } // последнее возникшее исключение

        public ChatServer(int _udpServerPort, int _tcpServerPort, int _backlogSize, string _serverIpAddress) {
            udpServerPort = _udpServerPort;
            tcpServerPort = _tcpServerPort;
            backlogSize = _backlogSize;
            serverIpAddress = _serverIpAddress;

            registeredUsers = new RegisteredUsers();
        }

        // запуск сервера для ответа на широковещательные запросы
        public bool LaunchUdpServer()
        {
            try
            {
                udpServer = new UdpClient(udpServerPort);

                udpServer.BeginReceive(UdpReceiveCallback,
                    new UdpState
                    {
                        endPoint = new IPEndPoint(IPAddress.Any, 0),
                        client = udpServer
                    });
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex;
                return false;
            }
        }

        // обработчик широковещательного запроса 
        private void UdpReceiveCallback(IAsyncResult asyncResult)
        {
            var state = (UdpState)(asyncResult.AsyncState);

            var u = state.client;
            var e = state.endPoint;

            var receiveBytes = u.EndReceive(asyncResult, ref e);
            var clientRequest = _utilities.GetStringFromBytesReceived(receiveBytes);

            _utilities.WriteMessageToConsole($"Получен запрос '{clientRequest}' от {e.Address.ToString()}, отправляем ответ.");

            var responseData = _utilities.GetBytesToSend($"{serverIpAddress}:{tcpServerPort}");
            udpServer.Send(responseData, responseData.Length, e);

            udpServer.BeginReceive(UdpReceiveCallback, state);
        }

        // запуск чат-сервера
        public bool LaunchChatServer()
        {
            var ipPoint = new IPEndPoint(IPAddress.Parse(serverIpAddress), tcpServerPort);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(backlogSize);
                listenSocket.BeginAccept(AcceptCallback, listenSocket);

                return true;
            }
            catch (Exception ex)
            {
                lastError = ex;
                return false;
            }
        }

        // обработчик приема соединения
        private void AcceptCallback(IAsyncResult asyncResult)
        {
            var srvSocket = (Socket)asyncResult.AsyncState;
            var clientSocket = srvSocket.EndAccept(asyncResult);
            clientSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, clientSocket);

            srvSocket.BeginAccept(AcceptCallback, srvSocket);
        }

        // главный обработчик запросов
        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            var socket = (Socket)asyncResult.AsyncState;
            var received = socket.EndReceive(asyncResult, out var se);
            if (se != SocketError.Success)
            {
                _utilities.WriteMessageToConsole("При получении сообщения от одного из клиентов произошла ошибка.", true, EventLevel.Error);
                return;
            }

            var tempBuffer = new byte[received];
            Array.Copy(dataBuffer, tempBuffer, received);
            var messageReceived = _utilities.GetStringFromBytesReceived(tempBuffer);
            _utilities.WriteMessageToConsole($"Получено сообщение: {messageReceived}");

            if (string.IsNullOrEmpty(messageReceived)) // приходит при отсоединении
                return;

            var command = new ChatCommand();
            command.TryParse(messageReceived, true, out var validationMessage);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                _utilities.WriteMessageToConsole(validationMessage, false, EventLevel.Error);
                return;
            }

            // обработаем полученную команду
            ProcessCommand(command, socket);

            if (needToCloseClientConnection)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            if (socket.Connected && !needToCloseClientConnection)
                socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, socket);
        }

        // метод обработки команды
        private void ProcessCommand(ChatCommand command, Socket socket)
        {
            var responseMessage = string.Empty;
            needToCloseClientConnection = false;
            var senderName = command.Arguments["SenderName"];

            switch (command.Type)
            {
                case CommandType.listusers:
                    responseMessage = !registeredUsers.IsUserRegistered(command.Arguments["SenderName"]) ? ChatCommand.msgUserNotRegistered : $"Подключенные пользователи: {registeredUsers.ToString()}";

                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
                case CommandType.exit:
                    if (!registeredUsers.IsUserRegistered(senderName))
                        responseMessage = ChatCommand.msgUserNotRegistered;
                    else
                    {
                        needToCloseClientConnection = true;
                        registeredUsers.RemoveUser(senderName);
                        responseMessage = "До свидания!";
                    }
                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
                case CommandType.register:
                    if (registeredUsers.IsUserRegistered(senderName))
                        responseMessage = $"Пользователь '{senderName}' уже зарегистрирован. Пожалуйста выберите другое имя.";
                    else
                    {
                        responseMessage = ChatCommand.msgYouAreSuccessfullyRegistered;
                        registeredUsers.AddUser(senderName, socket);
                    }

                    socket.Send(_utilities.GetBytesToSend(responseMessage));

                    break;
                case CommandType.message:
                    var userTo = command.Arguments["RecipientName"];
                    if (!registeredUsers.IsUserRegistered(senderName))
                    {
                        responseMessage = ChatCommand.msgUserNotRegistered;
                    }
                    else if (!registeredUsers.IsUserRegistered(userTo))
                    {
                        responseMessage = $"Получатель '{userTo}' не зарегистрирован на сервере.";
                    }
                    else
                    {
                        var receiverSocket = registeredUsers.GetUserSocket(userTo);
                        receiverSocket.Send(_utilities.GetBytesToSend($"Пользователь '{senderName}' прислал вам сообщение: '{command.Arguments["Message"]}'"));

                        responseMessage = "Ваше сообщение успешно отправлено.";
                    }
                    socket.Send(_utilities.GetBytesToSend(responseMessage));
                    break;
            }
        }

        public void ShutDown() {
            listenSocket.Close();
            udpServer.Close();

            registeredUsers.CloseConnections();
        }
    }
}
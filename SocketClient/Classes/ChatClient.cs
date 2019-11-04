using System;
using System.Net;
using System.Net.Sockets;
using Utilities;
using _utilities = Utilities.Utilities;

namespace SocketClient.Classes
{
    public class ChatClient
    {
        bool isServerKnown; // известен ли адрес сервера
        string serverAddress; // адрес сервера
        int serverPort; // порт сервера
        readonly byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        Socket _socket;
        public Exception lastError { get; private set; } // последнее возникшее исключение

        public ChatClient(bool _isServerKnown, string _serverAddress, int _serverPort) {
            isServerKnown = _isServerKnown;
            serverAddress = _serverAddress;
            serverPort = _serverPort;
        }

        // соединение с сервером
        public bool ConnectToServer()
        {
            if (!isServerKnown && !GetChatServerAddress())
                return false;

            var ipPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // подключаемся к серверу
            try
            {
                _socket.Connect(ipPoint);
            }
            catch (Exception ex)
            {
                lastError = ex;
                return false;
            }

            _socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);

            return _socket.Connected;
        }

        // отправка команды серверу
        public void SendCommand(ChatCommand command) {
            _socket.Send(_utilities.GetBytesToSend(command.ToString()));
        }

        // закрытие соединения
        public void CloseConnection()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        
        // получение адреса чат-сервера
        private bool GetChatServerAddress() {
            var client = new UdpClient();
            try
            {
                var requestData = _utilities.GetBytesToSend("GetServerChatAddress");
                var serverEp = new IPEndPoint(IPAddress.Any, 0);

                client.EnableBroadcast = true;
                client.Send(requestData, requestData.Length, new IPEndPoint(IPAddress.Broadcast, 8005));

                var serverResponseData = client.Receive(ref serverEp);
                var serverResponse = _utilities.GetStringFromBytesReceived(serverResponseData);

                _utilities.WriteMessageToConsole($"От {serverEp.Address} получен ответ '{serverResponse}'");
                var responseParts = serverResponse.Split(':');

                serverAddress = responseParts[0];
                serverPort = Convert.ToInt32(responseParts[1]);

                client.Close();
                return true;
            }
            catch (Exception ex) {
                lastError = ex;
                return false;
            }
        }

        // асинхронный обработчик приема сообщения
        private void ReceiveCallback(IAsyncResult asyncResult)
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

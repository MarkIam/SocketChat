using System;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using SocketServer.Classes;
using _utilities = Utilities.Utilities;

namespace SocketServer
{
    class Program
    {
        public const string localIpAddress = "127.0.0.1";

        private static string _serverIpAddress; // IP-адрес сервера
        private static int _udpServerPort; // порт для приема широковещательных запросов
        private static int _tcpServerPort; // порт для приема входящих TCP-запросов
        private static int _backlogSize; // размер беклога для сервера

        static ChatServer chatServer;

        static void Main(string[] args)
        {
            Console.Title = "Chat server";
            _utilities.SetConsoleEncoding();

            // считываем  настройки
            ReadSettings();

            chatServer = new ChatServer(_udpServerPort, _tcpServerPort, _backlogSize, _serverIpAddress);

            // запускаем сервер для ответа на широковещательные запросы
            if (chatServer.LaunchUdpServer())
                _utilities.WriteMessageToConsole("Запущен сокет для широковещательных запросов на поиск чат-сервера.");
            else
                _utilities.WriteMessageToConsole($"При запуске сокета для широковещательных запросов произошла ошибка: {chatServer.lastError.Message}", true, EventLevel.Error);

            // запускаем чат-сервер
            if (chatServer.LaunchChatServer())
                _utilities.WriteMessageToConsole("Чат-сервер запущен и ожидает подключений...");
            else
                _utilities.WriteMessageToConsole($"При запуске чат-сервера произошла ошибка: {chatServer.lastError.Message}", true, EventLevel.Error);

            Console.ReadLine();

            chatServer.CloseSockets();
        }

        // считывание настроек
        static void ReadSettings()
        {
            _udpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["UdpServerPort"]);
            _tcpServerPort = Convert.ToInt32(ConfigurationManager.AppSettings["TcpServerPort"]);
            _backlogSize = Convert.ToInt32(ConfigurationManager.AppSettings["BacklogSize"]);

            try
            {
                _serverIpAddress = GetLocalIpAddress();
            }
            catch (Exception ex) {
                _serverIpAddress = localIpAddress;
                _utilities.WriteMessageToConsole($"Работа сервера возможна только в рамках одной рабочей станции. {ex.Message}", true, EventLevel.Warning);
            }
        }

        // получение локального IP-адреса
        private static string GetLocalIpAddress()
        {
            //bool first = true;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    //if (first)
                    //{
                    //    first = false;
                    //    continue;
                    //}
                    //else
                        return ip.ToString();
            throw new Exception("Сетевые адаптеры не обнаружены.");
        }
    }
}
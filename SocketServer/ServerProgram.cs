using System;
using System.Configuration;
using System.Diagnostics.Tracing;
using SocketServer.Classes;
using _utilities = Utilities.Utilities;

namespace SocketServer
{
    class Program
    {
        private static string _serverIpAddress; // IP-адрес сервера
        private static int _udpServerPort; // порт для приема широковещательных запросов
        private static int _tcpServerPort; // порт для приема входящих TCP-запросов
        private static int _backlogSize; // размер беклога для сервера

        static void Main(string[] args)
        {
            Console.Title = "Chat server";
            _utilities.SetConsoleEncoding();

            // считываем  настройки
            ReadSettings();

            var chatServer = new ChatServer(_udpServerPort, _tcpServerPort, _backlogSize, _serverIpAddress);

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

            chatServer.ShutDown();
        }

        // считывание настроек
        static void ReadSettings()
        {
            var settings = ConfigurationManager.AppSettings;

            _udpServerPort = Convert.ToInt32(settings["UdpServerPort"]);
            _tcpServerPort = Convert.ToInt32(settings["TcpServerPort"]);
            _backlogSize = Convert.ToInt32(settings["BacklogSize"]);

            try
            {
                _serverIpAddress = _utilities.GetLocalIpAddress();
            }
            catch (Exception ex) {
                _serverIpAddress = _utilities.localIpAddress;
                _utilities.WriteMessageToConsole($"Работа сервера возможна только в рамках одной рабочей станции. {ex.Message}", true, EventLevel.Warning);
            }
        }
    }
}
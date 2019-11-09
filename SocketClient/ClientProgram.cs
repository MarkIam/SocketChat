using System;
using System.Configuration;
using Utilities;
using _utilities = Utilities.Utilities;
using EventLevel = System.Diagnostics.Tracing.EventLevel;
using SocketClient.Classes;

namespace SocketClient
{
    class Program
    {
        // адрес и порт сервера, к которому будем подключаться
        static bool _isServerKnown; // известен ли адрес сервера
        static string _serverAddress; // адрес сервера
        static int _serverPort; // порт сервера
        static string _userName; // имя пользователя
        static readonly byte[] dataBuffer = new byte[256]; // буфер для получаемых данных
        static ChatClient chatClient;

        // считывание настроек
        static void ReadSettings()
        {
            var settings = ConfigurationManager.AppSettings;

            _isServerKnown = settings["isServerAddressKnown"] == "1";
            _serverAddress = settings["ServerIpAddress"];
            _serverPort = Convert.ToInt32(settings["ServerPort"]);
        }

        static void Main(string[] args)
        {
            Console.Title = "Chat client";
            _utilities.SetConsoleEncoding();

            // считываем  настройки
            ReadSettings();

            chatClient = new ChatClient(_isServerKnown, _serverAddress, _serverPort);

            try
            {
                // соединяемся с сервером
                var connected = chatClient.ConnectToServer();

                if (connected)
                    _utilities.WriteMessageToConsole("Успешное подключение к серверу");
                else
                {
                    _utilities.WriteMessageToConsole($"Не удалось установить соединение с сервером. Ошибка: {chatClient.lastError}", true, EventLevel.Error);
                    return;
                }

                var command = new ChatCommand();
                while (true)
                {
                    _utilities.WriteMessageToConsole("Введите команду:");
                    var inputString = Console.ReadLine();

                    if (!command.TryParse(inputString, false, out var validationMessage))
                    {
                        _utilities.WriteMessageToConsole(validationMessage, false, EventLevel.Error);
                        if (command.Type == CommandType.unknown)
                            WriteHelp();
                        continue;
                    }

                    if (command.Type == CommandType.register)
                    {
                        _userName = command.Arguments["SenderName"];
                        Console.Title = $"Chat client ({_userName})";
                    }

                    if (command.Type == CommandType.exit)
                    {
                        chatClient.SendCommand(command);
                        break;
                    }
                    if (command.Type != CommandType.help)
                        chatClient.SendCommand(command);
                    else
                        WriteHelp();
                }
            }
            catch (Exception ex)
            {
                _utilities.WriteMessageToConsole($"Произошла ошибка: {ex.Message}", true, EventLevel.Error);
            }
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

    }
}
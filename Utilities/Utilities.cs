using System;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Utilities
{
    // класс со вспомогательными методами
    public class Utilities
    {
        public const string localIpAddress = "127.0.0.1";

        // кодировка, используемая для обмена
        private static readonly Encoding _exchangeEncoding = Encoding.UTF8;

        // вывод сообщений в консоль
        public static void WriteMessageToConsole(string message, bool needToAddTimeInfo = true, EventLevel level = EventLevel.Informational)
        {
            var initialColor = Console.ForegroundColor;
            var newColor = ConsoleColor.White;
            switch (level)
            {
                case EventLevel.Critical:
                case EventLevel.Error:
                    newColor = ConsoleColor.Red;
                    break;
                case EventLevel.Warning:
                    newColor = ConsoleColor.Yellow;
                    break;
                case EventLevel.Informational:
                    newColor = ConsoleColor.White;
                    break;
                default:
                    newColor = ConsoleColor.White;
                    break;
            }
            Console.ForegroundColor = newColor;
            Console.WriteLine($"{(needToAddTimeInfo ? $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: " : string.Empty)}{message}");
            Console.ForegroundColor = initialColor;
        }

        public static void SetConsoleEncoding()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static byte[] GetBytesToSend(string messageToSend)
        {
            return _exchangeEncoding.GetBytes(messageToSend);
        }

        public static string GetStringFromBytesReceived(byte[] messageReceivedBytes)
        {
            return _exchangeEncoding.GetString(messageReceivedBytes);
        }

        // получение локального IP-адреса
        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new Exception("Сетевые адаптеры не обнаружены.");
        }
    }
}

using System;
using System.Diagnostics.Tracing;
using System.Text;

namespace Utilities
{
    // класс со вспомогательными методами
    public class Utilities
    {
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
    }
}

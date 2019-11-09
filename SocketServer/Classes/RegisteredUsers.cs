using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SocketServer.Classes
{
    internal class RegisteredUsers
    {
        // справочник клиентских сокетов по именам пользователей
        private Dictionary<string, Socket> dictSocketsByUserName;

        public RegisteredUsers()
        {
            dictSocketsByUserName = new Dictionary<string, Socket>();
        }

        // проверка наличия пользователя в списке зарегистрированных
        public bool IsUserRegistered(string userName)
        {
            return dictSocketsByUserName.ContainsKey(userName);
        }

        // Получение списка зарегистрированных пользователей
        public override string ToString()
        {
            return dictSocketsByUserName.Keys.ToList().Aggregate((a, b) => $"{a}, {b}");
        }

        // удаление пользователя
        public void RemoveUser(string userName)
        {
            if (!IsUserRegistered(userName))
                throw new Exception($"Пользователь {userName} не найден в списке.");

            dictSocketsByUserName.Remove(userName);
        }

        // добавление пользователя
        public void AddUser(string userName, Socket socket)
        {
            if (IsUserRegistered(userName))
                throw new Exception($"Пользователь {userName} уже присутствует в списке.");

            dictSocketsByUserName.Add(userName, socket);
        }

        // получение соединения пользователя
        public Socket GetUserSocket(string userName)
        {
            return dictSocketsByUserName[userName];
        }

        // закрытие всех соединений при  выходе
        public void CloseConnections()
        {
            foreach (var client in dictSocketsByUserName)
            {
                var socket = client.Value;
                // TODO: можно не рвать соединения, а отправлять сообщение о остановке сервера
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}

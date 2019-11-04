using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SocketServer.Classes
{
    internal class RegisteredUsers
    {
        // справочник клиентских сокетов по именам пользователей
        private Dictionary<string, Socket> _dictSocketsByUserName;

        public RegisteredUsers()
        {
            _dictSocketsByUserName = new Dictionary<string, Socket>();
        }

        // проверка наличия пользователя в списке зарегистрированных
        public bool IsUserRegistered(string userName)
        {
            return _dictSocketsByUserName.ContainsKey(userName);
        }

        // Получение списка зарегистрированных пользователей
        public override string ToString()
        {
            return _dictSocketsByUserName.Keys.ToList().Aggregate((a, b) => $"{a}, {b}");
        }

        // удаление пользователя
        public void RemoveUser(string userName)
        {
            if (!IsUserRegistered(userName))
                throw new Exception($"Пользователь {userName} не найден в списке.");

            _dictSocketsByUserName.Remove(userName);
        }

        // добавление пользователя
        public void AddUser(string userName, Socket socket)
        {
            if (IsUserRegistered(userName))
                throw new Exception($"Пользователь {userName} уже присутствует в списке.");

            _dictSocketsByUserName.Add(userName, socket);
        }

        // получение соединения пользователя
        public Socket GetUserSocket(string userName)
        {
            return _dictSocketsByUserName[userName];
        }
    }
}

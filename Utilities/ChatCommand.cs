using System;
using System.Collections.Generic;
using System.Linq;

namespace Utilities
{
    // перечисление типов команд
    public enum CommandType
    {
        exit,
        help,
        listusers,
        message,
        register,
        unknown
    }

    // класс для работы с командой
    public class ChatCommand
    {
        public const string msgUserNotRegistered = "Вы не зарегистрированы на сервере.";
        public const string msgCantBeSentToYourself = "Сообщение не может быть отправлено самому себе.";
        public const string msgUserIsAlreadyRegistered = "Вы уже зарегистрированы.";
        public const string msgYouAreSuccessfullyRegistered = "Вы успешно зарегистрированы.";
        public const string msgUnknownCommand = "Неизвестная команда.";
        public const string msgWrongParametersNumber = "Передано ошибочное число параметров для команды";

        private static readonly Dictionary<CommandType, short> numberOfArgumentsByCommandType =
            new Dictionary<CommandType, short>
            {
                [CommandType.exit] = 1, // имя пользователя
                [CommandType.help] = 0,
                [CommandType.listusers] = 1, // имя пользователя
                [CommandType.message] = 3, // получатель / сообщение / отправитель
                [CommandType.register] = 1, // имя пользователя
            };

        public CommandType Type { get; private set; }
        public Dictionary<string, string> Arguments { get; private set; }
        public string UserName { get; private set; }

        public bool TryParse(string inputString, out string validationMessage)
        {
            var messageParts = inputString.Split(new char[] {' '} , StringSplitOptions.RemoveEmptyEntries);
            var commandString = messageParts[0].ToLowerInvariant();

            validationMessage = string.Empty;
            if (!Enum.TryParse(commandString, out CommandType commandType) || commandType == CommandType.unknown)
            {
                validationMessage = msgUnknownCommand;
                return false;
            }

            var arguments = messageParts.Skip(1);
            var numberOfArguments = numberOfArgumentsByCommandType[commandType];

            if (numberOfArguments != arguments.Count())
            {
                validationMessage = $"{msgWrongParametersNumber} {commandString}";
                return false;
            }

            var commandArguments = new Dictionary<string, string>();
            switch (commandType)
            {
                case CommandType.message:
                    commandArguments.Add("RecipientName", arguments.ElementAt(0));
                    commandArguments.Add("Message", arguments.ElementAt(1));
                    commandArguments.Add("UserName", arguments.ElementAt(2));
                    if (string.IsNullOrEmpty(UserName))
                    {
                        validationMessage = ChatCommand.msgUserNotRegistered;
                        return false;
                    }
                    else if (commandArguments["RecipientName"] == UserName)
                    {
                        validationMessage = ChatCommand.msgCantBeSentToYourself;
                        return false;
                    }
                    break;
                case CommandType.register:
                    if (!string.IsNullOrEmpty(UserName))
                    {
                        validationMessage = msgUserIsAlreadyRegistered;
                        return false;
                    }

                    commandArguments.Add("UserName", arguments.ElementAt(0));
                    UserName = arguments.ElementAt(0);
                    break;
                case CommandType.exit:
                case CommandType.listusers:
                    if (string.IsNullOrEmpty(UserName))
                    {
                        validationMessage = msgUserNotRegistered;
                        return false;
                    }
                    commandArguments.Add("UserName", arguments.ElementAt(0));
                    break;
            }

            Type = commandType;
            Arguments = commandArguments;
            UserName = commandArguments["UserName"];

            return true;
        }

        public void AddArguments(string argumentName, string argumentValue)
        {
            if (!Arguments.ContainsKey(argumentName))
                Arguments.Add(argumentName, argumentValue);
            else
                Arguments[argumentName] = argumentValue;
        }

        public override string ToString()
        {
            return $"{Type.ToString()} {Arguments.Values.Aggregate((a, b) => $"{a} {b}")}";
        }
    }
}

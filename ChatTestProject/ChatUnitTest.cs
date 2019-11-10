using System;
using System.Configuration;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketClient.Classes;
using SocketServer.Classes;
using Utilities;
using _utilities = Utilities.Utilities;

namespace ChatTestProject
{
    [TestClass]
    public class ChatUnitTest
    {
        private int udpServerPort;
        private int tcpServerPort;
        private int backlogSize;
        private string serverIpAddress;
        private bool isServerKnown;
        private string serverAddress;
        private int serverPort;

        void ReadSettings()
        {
            var settings = ConfigurationManager.AppSettings;

            udpServerPort = Convert.ToInt32(settings["UdpServerPort"]);
            tcpServerPort = Convert.ToInt32(settings["TcpServerPort"]);
            backlogSize = Convert.ToInt32(settings["BacklogSize"]);

            try
            {
                serverIpAddress = _utilities.GetLocalIpAddress();
            }
            catch (Exception ex)
            {
                serverIpAddress = _utilities.localIpAddress;
            }

            isServerKnown = settings["isServerAddressKnown"] == "1";
            serverAddress = settings["ServerIpAddress"];
            serverPort = Convert.ToInt32(settings["ServerPort"]);
        }


        [TestMethod]
        public void TestCommandSyntax()
        {
            string validationMessage;
            var cmd = new ChatCommand();

            cmd.TryParse("somecommand", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgUnknownCommand);

            cmd.TryParse("register", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("register user1", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.register);

            cmd.TryParse("listusers", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.listusers);

            cmd.TryParse("message Hello", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("message user2 Hello", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.message);
            Assert.IsTrue(cmd.Arguments["Message"] == "Hello" && cmd.Arguments["RecipientName"] == "user2");

            cmd.TryParse("help someparameter", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("exit", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.exit);
        }

        private string messageFromServer;

        [TestMethod]
        public void TestIntegration()
        {
            var timeout = 1000;
            ReadSettings();

            var srv = new ChatServer(udpServerPort, tcpServerPort, backlogSize, serverIpAddress);
            Assert.IsTrue(srv.LaunchUdpServer());
            Assert.IsTrue(srv.LaunchChatServer());

            var clnt1 = new ChatClient(isServerKnown, serverAddress, serverPort);
            //clnt1.OnMessage += (msg) => { messageFromServer = msg; };

            Assert.IsTrue(clnt1.ConnectToServer());

            var validationMessage = string.Empty;
            var command1 = new ChatCommand();
            Assert.IsTrue(command1.TryParse("register user1", false, out validationMessage));

            clnt1.SendCommand(command1);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == ChatCommand.msgYouAreSuccessfullyRegistered);

            Assert.IsTrue(command1.TryParse("listusers", false, out validationMessage));
            clnt1.SendCommand(command1);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == "Подключенные пользователи: user1");

            var clnt2 = new ChatClient(isServerKnown, serverAddress, serverPort);
            Assert.IsTrue(clnt2.ConnectToServer());

            var command2 = new ChatCommand();
            Assert.IsTrue(command2.TryParse("register user2", false, out validationMessage));

            clnt2.SendCommand(command2);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt2.LastMessageReceivedFromServer == ChatCommand.msgYouAreSuccessfullyRegistered);

            Assert.IsTrue(command2.TryParse("listusers", false, out validationMessage));
            clnt2.SendCommand(command2);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt2.LastMessageReceivedFromServer == "Подключенные пользователи: user1, user2");

            Assert.IsTrue(command1.TryParse("message user2 123", false, out validationMessage));
            clnt1.SendCommand(command1);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt2.LastMessageReceivedFromServer == "Пользователь 'user1' прислал вам сообщение: '123'");

            Assert.IsTrue(command2.TryParse("message user1 456", false, out validationMessage));
            clnt2.SendCommand(command2);
            Thread.Sleep(timeout);
            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == "Пользователь 'user2' прислал вам сообщение: '456'");

            srv.ShutDown();
        }

    }
    }

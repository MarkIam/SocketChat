using System;
using System.Configuration;
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

            cmd.TryParse("register Mark", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.register);

            cmd.TryParse("listusers", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.listusers);

            cmd.TryParse("message Hello", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("message Olya Hello", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.message);
            Assert.IsTrue(cmd.Arguments["Message"] == "Hello" && cmd.Arguments["RecipientName"] == "Olya");

            cmd.TryParse("help someparameter", false, out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("exit", false, out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.exit);
        }

        private string messageFromServer;

        [TestMethod]
        public void TestIntegration()
        {
            ReadSettings();

            var srv = new ChatServer(udpServerPort, tcpServerPort, backlogSize, serverIpAddress);
            Assert.IsTrue(srv.LaunchUdpServer());
            Assert.IsTrue(srv.LaunchChatServer());

            var clnt1 = new ChatClient(isServerKnown, serverAddress, serverPort);

            //clnt1.OnMessage += (msg) => { messageFromServer = msg; };

            Assert.IsTrue(clnt1.ConnectToServer());

            var validationMessage = string.Empty;
            var command1 = new ChatCommand();
            Assert.IsTrue(command1.TryParse("register mark", false, out validationMessage));
            clnt1.SendCommand(command1);

            Assert.IsTrue(command1.TryParse("listusers", false, out validationMessage));
            clnt1.SendCommand(command1);

            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == "Подключенные пользователи: mark");

            var clnt2 = new ChatClient(isServerKnown, serverAddress, serverPort);
            Assert.IsTrue(clnt1.ConnectToServer());

            var command2 = new ChatCommand();
            Assert.IsTrue(command2.TryParse("register olya", false, out validationMessage));
            clnt2.SendCommand(command2);

            Assert.IsTrue(command2.TryParse("listusers", false, out validationMessage));
            clnt2.SendCommand(command2);

            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == "Подключенные пользователи: mark, olya");

            Assert.IsTrue(command1.TryParse("message olya 123", false, out validationMessage));
            clnt1.SendCommand(command1);


            Assert.IsTrue(command2.TryParse("message mark 456", false, out validationMessage));
            clnt2.SendCommand(command2);

            Assert.IsTrue(clnt1.LastMessageReceivedFromServer == "456");

            srv.ShutDown();
        }

    }
    }

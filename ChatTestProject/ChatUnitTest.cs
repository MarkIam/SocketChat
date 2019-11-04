using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities;

namespace ChatTestProject
{
    [TestClass]
    public class ChatUnitTest
    {
        [TestMethod]
        public void TestCommandSyntax()
        {
            string validationMessage;
            var cmd = new ChatCommand();

            cmd.TryParse("somecommand", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgUnknownCommand);

            cmd.TryParse("register", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("register Mark", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.register);

            cmd.TryParse("listusers", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.listusers);

            cmd.TryParse("message Hello", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.TryParse("message Hello Olya", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.message);
            Assert.IsTrue(cmd.Arguments["Message"] == "Hello" && cmd.Arguments["RecipientName"] == "Olya");

            cmd.TryParse("help someparameter", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgUnknownCommand);

            cmd.TryParse("exit", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.exit);
        }
    }
}

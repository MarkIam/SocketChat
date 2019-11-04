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

            cmd.Parse("somecommand", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgUnknownCommand);

            cmd.Parse("register", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.Parse("register Mark", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.register);

            cmd.Parse("listusers", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.listusers);

            cmd.Parse("message Hello", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgWrongParametersNumber);

            cmd.Parse("message Hello Olya", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.message);
            Assert.IsTrue(cmd.Arguments["Message"] == "Hello" && cmd.Arguments["RecipientName"] == "Olya");

            cmd.Parse("help someparameter", out validationMessage);
            Assert.AreEqual(validationMessage, ChatCommand.msgUnknownCommand);

            cmd.Parse("exit", out validationMessage);
            Assert.AreEqual(cmd.Type, CommandType.exit);
        }
    }
}

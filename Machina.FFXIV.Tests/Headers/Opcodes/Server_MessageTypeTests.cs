using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina.FFXIV.Headers.Opcodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina.FFXIV.Headers.Opcodes.Tests
{
    [TestClass()]
    public class Server_MessageTypeTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            OpcodeManager.Instance.SetRegion(GameRegionEnum.Global);
        }
        [TestMethod()]
        public void Server_MessageType_Equals()
        {
            var sut = Server_MessageType.Ability1;

            var result = sut.Equals(Server_MessageType.Ability1);

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void Server_MessageType_NotEquals()
        {
            var sut = Server_MessageType.Ability1;

            var result = sut.Equals(Server_MessageType.Ability8);

            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void Server_MessageType_TypeCastToFromUShort()
        {
            var sut = Server_MessageType.Ability1;
            var test = (ushort)sut;
            var result = sut.Equals((Server_MessageType)test);

            Assert.IsTrue(result);
        }
    }
}
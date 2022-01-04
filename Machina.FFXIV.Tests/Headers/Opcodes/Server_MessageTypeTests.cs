// Copyright © 2021 Ravahn - All Rights Reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see<http://www.gnu.org/licenses/>.

using Machina.FFXIV.Headers;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.FFXIV.Tests.Headers.Opcodes
{
    [TestClass()]
    public class Server_MessageTypeTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext _1)
        {
            OpcodeManager.Instance.SetRegion(GameRegion.Global);
        }
        [TestMethod()]
        public void Server_MessageType_Equals()
        {
            Server_MessageType sut = Server_MessageType.Ability1;

            bool result = sut.Equals(Server_MessageType.Ability1);

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void Server_MessageType_NotEquals()
        {
            Server_MessageType sut = Server_MessageType.Ability1;

            bool result = sut.Equals(Server_MessageType.Ability8);

            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void Server_MessageType_TypeCastToFromUShort()
        {
            Server_MessageType sut = Server_MessageType.Ability1;
            ushort test = sut;
            bool result = sut.Equals((Server_MessageType)test);

            Assert.IsTrue(result);
        }
    }
}

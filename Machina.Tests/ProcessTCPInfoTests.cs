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

using System.Collections.Generic;
using Machina.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.Tests
{
    [TestClass()]
    public class ProcessTCPInfoTests
    {
        [TestMethod()]
        public void GetProcessIDByWindow_WindowNameTest()
        {

            IList<uint> result = ProcessTCPInfo.GetProcessIDByWindow(null, "Program Manager");

            Assert.AreEqual(1, result.Count);
        }


        [TestMethod()]
        public void GetProcessIDByWindow_WindowClassTest()
        {
            IList<uint> result = ProcessTCPInfo.GetProcessIDByWindow("Progman", null);

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod()]
        public void UpdateTCPIPConnectionsTest()
        {
            ProcessTCPInfo sut = new();

            List<TCPConnection> connections = new();

            sut.ProcessWindowName = "Program Manager";
            sut.UpdateTCPIPConnections(connections);

            Assert.IsTrue(connections.Count >= 0);
        }
    }
}

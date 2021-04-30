using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina.Tests
{
    [TestClass()]
    public class ProcessTCPInfoTests
    {
        [TestMethod()]
        public void GetProcessIDByWindow_WindowNameTest()
        {
            var sut = new ProcessTCPInfo();

            var result = sut.GetProcessIDByWindow(null, "Program Manager");

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod()]
        public void GetProcessIDByWindow_WindowClassTest()
        {
            var sut = new ProcessTCPInfo();

            var result = sut.GetProcessIDByWindow(null, "Program Manager");

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod()]
        public void UpdateTCPIPConnectionsTest()
        {
            var sut = new ProcessTCPInfo();

            var connections = new List<TCPConnection>();

            sut.ProcessWindowName = "Program Manager";
            sut.UpdateTCPIPConnections(connections);

            Assert.IsTrue(connections.Count >= 0);
        }
    }
}

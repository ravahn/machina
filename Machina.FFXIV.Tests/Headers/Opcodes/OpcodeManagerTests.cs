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
    public class OpcodeManagerTests
    {
        [TestMethod()]
        public void OpcodeManagerTest()
        {
            var sut = new OpcodeManager();

            Assert.IsNotNull(sut);
        }

        [TestMethod()]
        public void SetVersionTest()
        {
            var sut = new OpcodeManager();

            sut.SetVersion(5.10f);

            Assert.AreEqual(5.10f, sut.Version);
        }
    }
}
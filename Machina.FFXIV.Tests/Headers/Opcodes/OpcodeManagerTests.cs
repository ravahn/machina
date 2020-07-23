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
        public void SetRegionTest()
        {
            var sut = new OpcodeManager();

            sut.SetRegion(GameRegionEnum.Korean);

            Assert.AreEqual(GameRegionEnum.Korean, sut.GameRegion);
            Assert.AreEqual(sut.CurrentOpcodes["ActorControl142"], 0x142);
        }
    }
}
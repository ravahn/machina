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

using Machina.FFXIV.Headers.Opcodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.FFXIV.Tests.Headers.Opcodes
{
    [TestClass()]
    public class OpcodeManagerTests
    {
        [TestMethod()]
        public void OpcodeManagerTest()
        {
            OpcodeManager sut = new();

            Assert.IsNotNull(sut);
        }

        [TestMethod()]
        public void SetRegionTest()
        {
            OpcodeManager sut = new();

            sut.SetRegion(GameRegion.Korean);

            Assert.AreEqual(GameRegion.Korean, sut.GameRegion);
            Assert.IsTrue(sut.CurrentOpcodes["ActorControl"] > 0);
        }
    }
}

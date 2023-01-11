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
using Machina.FFXIV.Oodle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Machina.FFXIV.Tests.Oodle
{
    [TestClass()]
    public class OodleTests
    {
        [TestMethod()]
        public void Oodle_ConstructorTest()
        {
            Moq.Mock<IOodleNative> mockOodleNative = new();
            _ = mockOodleNative.Setup(x => x.OodleNetwork1UDP_State_Size()).Returns(1);
            _ = mockOodleNative.Setup(x => x.OodleNetwork1_Shared_Size(Moq.It.IsAny<int>())).Returns(1);

            FFXIV.Oodle.OodleUDPWrapper sut = new(mockOodleNative.Object);

            Assert.IsNotNull(sut);
        }

        [TestMethod()]
        public void Oodle_DecompressTest()
        {
            Moq.Mock<IOodleNative> mockOodleNative = new();
            _ = mockOodleNative.Setup(x => x.OodleNetwork1UDP_State_Size()).Returns(1);
            _ = mockOodleNative.Setup(x => x.OodleNetwork1_Shared_Size(Moq.It.IsAny<int>())).Returns(1);
            _ = mockOodleNative.Setup(x => x.OodleNetwork1UDP_Decode(Moq.It.IsAny<byte[]>(), Moq.It.IsAny<byte[]>(),
                Moq.It.IsAny<IntPtr>(), Moq.It.IsAny<int>(), Moq.It.IsAny<byte[]>(), Moq.It.IsAny<int>())).Returns(true);

            FFXIV.Oodle.OodleUDPWrapper sut = new(mockOodleNative.Object);

            bool result = sut.Decompress(null, 0, 0, null, 0);

            Assert.IsTrue(result);
        }
    }
}

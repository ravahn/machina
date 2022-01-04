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

using System;
using Machina.Infrastructure;
using Machina.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.Tests
{
    [TestClass()]
    public class ConversionUtilityTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }

        [TestMethod()]
        public void Utility_ByteArrayToHexStringTest_SkipBytesShortbuffer()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            string result = ConversionUtility.ByteArrayToHexString(data, 5, 4);

            Assert.AreEqual("06070809", result);
        }

        [TestMethod()]
        public void Utility_HexStringToByteArrayTest()
        {
            string data = "0102030405060708090a0b0c0d0e0f1011121314";

            byte[] result = ConversionUtility.HexStringToByteArray(data);

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(i + 1, result[i]);
        }

        [TestMethod()]
        public void Utility_EpochToDateTimeTest()
        {
            DateTime currentTime = DateTime.Now;

            long epoch = (long)currentTime.Subtract(DateTime.Parse("1/1/1970")).TotalMilliseconds;
            DateTime result = ConversionUtility.EpochToDateTime(epoch);

            Assert.AreEqual(0, (long)currentTime.Subtract(result).TotalMilliseconds);
        }

        [TestMethod()]
        public void Utility_ntohsTest()
        {
            ushort data = 0x1234;

            ushort result = ConversionUtility.ntohs(data);
            Assert.AreEqual((ushort)0x3412, result);
        }

        [TestMethod()]
        public void Utility_ntohlTest()
        {
            uint data = 0x12345678;

            uint result = ConversionUtility.ntohl(data);
            Assert.AreEqual((uint)0x78563412, result);
        }

        [TestMethod()]
        public void Utility_ntohqTest()
        {
            ulong data = 0x1122334455667788;

            ulong result = ConversionUtility.ntohq(data);
            Assert.AreEqual(0x8877665544332211, result);
        }

        [TestMethod()]
        public void Utility_htonsTest()
        {
            ushort data = 0x1234;

            ushort result = ConversionUtility.htons(data);
            Assert.AreEqual((ushort)0x3412, result);
        }

        [TestMethod()]
        public void Utility_htonlTest()
        {
            uint data = 0x12345678;

            uint result = ConversionUtility.htonl(data);
            Assert.AreEqual((uint)0x78563412, result);
        }

        [TestMethod()]
        public void Utility_htonqTest()
        {
            ulong data = 0x1122334455667788;

            ulong result = ConversionUtility.htonq(data);
            Assert.AreEqual(0x8877665544332211, result);
        }

        [TestMethod()]
        public void Utility_GetNetworkInterfacesTest()
        {
            System.Collections.Generic.IList<string> result = InterfaceHelper.GetNetworkInterfaceIPs();

            Assert.IsTrue(result.Contains("127.0.0.1"));
        }
    }
}

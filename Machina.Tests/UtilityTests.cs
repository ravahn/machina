// Machina.Tests ~ Utility.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina;
using System;

namespace Machina.Tests
{
    [TestClass()]
    public class UtilityTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }


        [TestMethod()]
        public void Utility_ByteArrayToHexStringTest()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            string result = Utility.ByteArrayToHexString(data);

            Assert.AreEqual("0102030405060708090A", result);
        }

        [TestMethod()]
        public void Utility_ByteArrayToHexStringTest_SkipBytes()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            string result = Utility.ByteArrayToHexString(data, 5);

            Assert.AreEqual("060708090A", result);
        }

        [TestMethod()]
        public void Utility_ByteArrayToHexStringTest_SkipBytesShortbuffer()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            string result = Utility.ByteArrayToHexString(data, 5, 4);

            Assert.AreEqual("06070809", result);
        }

        [TestMethod()]
        public void Utility_HexStringToByteArrayTest()
        {
            string data = "0102030405060708090a0b0c0d0e0f1011121314";

            var result = Utility.HexStringToByteArray(data);

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(i+1, result[i]);
        }

        [TestMethod()]
        public void Utility_EpochToDateTimeTest()
        {
            DateTime currentTime = DateTime.Now;

            long epoch = (long)(currentTime.Subtract(DateTime.Parse("1/1/1970")).TotalMilliseconds);
            DateTime result = Utility.EpochToDateTime(epoch);

            Assert.AreEqual(0, (long)currentTime.Subtract(result).TotalMilliseconds);
        }

        [TestMethod()]
        public void Utility_ntohsTest()
        {
            ushort data = 0x1234;

            var result = Utility.ntohs(data);
            Assert.AreEqual((ushort)0x3412, result);
        }

        [TestMethod()]
        public void Utility_ntohlTest()
        {
            uint data = 0x12345678;

            var result = Utility.ntohl(data);
            Assert.AreEqual((uint)0x78563412, result);
        }

        [TestMethod()]
        public void Utility_ntohqTest()
        {
            ulong data = 0x1122334455667788;

            var result = Utility.ntohq(data);
            Assert.AreEqual((ulong)0x8877665544332211, result);
        }

        [TestMethod()]
        public void Utility_htonsTest()
        {
            ushort data = 0x1234;

            var result = Utility.htons(data);
            Assert.AreEqual((ushort)0x3412, result);
        }

        [TestMethod()]
        public void Utility_htonlTest()
        {
            uint data = 0x12345678;

            var result = Utility.htonl(data);
            Assert.AreEqual((uint)0x78563412, result);
        }

        [TestMethod()]
        public void Utility_htonqTest()
        {
            ulong data = 0x1122334455667788;

            var result = Utility.htonq(data);
            Assert.AreEqual((ulong)0x8877665544332211, result);
        }

        [TestMethod()]
        public void Utility_GetNetworkInterfacesTest()
        {
            var result = Utility.GetNetworkInterfaceIPs();

            Assert.IsTrue(result.Contains("127.0.0.1"));
        }
    }
}
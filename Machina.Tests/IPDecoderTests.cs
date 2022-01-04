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
using System.Runtime.InteropServices;
using Machina.Decoders;
using Machina.Headers;
using Machina.Infrastructure;
using Machina.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.Tests
{
    [TestClass]
    public class IPDecoderTests
    {

        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }


        [TestMethod]
        public void IPDecoder_FilterAndStoreData_OneIP4Packet()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_OneIP6Packet()
        {
            byte[] data = ConstructIP6Packet(
                6, 0, 0,
                0, 0, 0, 0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            IPDecoder sut = new IPDecoder(1, 1, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_InvalidVersionPacket()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                2,  // invalid ip version 2
                1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(1, TestInfrastructure.Listener.Messages.Count);
            Assert.IsTrue(TestInfrastructure.Listener.Messages[0].Contains("protocol version is neither"));
        }

        /// <summary>
        /// this tests successful processing of a single IP4 packet where the local pc is using hardware optimization.
        /// </summary>
        [TestMethod]
        public void IPDecoder_FilterAndStoreData_IP4PacketHardwareOffload()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                true
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        /// <summary>
        /// this tests failed processing of a single IP packet where the protocol is not TCP.
        /// </summary>
        [TestMethod]
        public void IPDecoder_FilterAndStoreData_FilterNonTCP()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.UDP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_IP4PacketLengthTooLong()
        {
            // this tests failed processing if the packet length is longer than the buffer 
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            // override packet length
            data[2] = 0xff;

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(1, TestInfrastructure.Listener.Messages.Count);
            Assert.IsTrue(TestInfrastructure.Listener.Messages[0].Contains("buffer too small"));
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_IP4PacketBufferTooLong()
        {
            // this does not fail processing if the packet length is longer than the buffer 
            //  this happens for ex. with winpcap.
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            // extend length of buffer
            Array.Resize(ref data, data.Length + 5);

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);

            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
            //Assert.IsTrue(TestInfrastructure.Listener.Messages[0].Contains("extra bytes"));
        }
        [TestMethod]
        public void IPDecoder_FilterAndStoreData_MultipleIP4Packets()
        {
            // this tests failed processing if the packet length is longer than the buffer 
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            int originalSize = data1.Length;
            Array.Resize(ref data1, data1.Length + data2.Length);
            Array.Copy(data2, 0, data1, originalSize, data2.Length);

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);

            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_MultipleIP6Packets()
        {
            byte[] data1 = ConstructIP6Packet(
                6, 0, 0,
                0, 0, 0, 0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );
            byte[] data2 = ConstructIP6Packet(
                6, 0, 0,
                0, 0, 0, 0,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            int originalSize = data1.Length;
            Array.Resize(ref data1, data1.Length + data2.Length);
            Array.Copy(data2, 0, data1, originalSize, data2.Length);

            IPDecoder sut = new IPDecoder(1, 1, IPProtocol.TCP);
            sut.FilterAndStoreData(data1, data1.Length);

            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data3 = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            byte[] data4 = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            originalSize = data3.Length;
            Array.Resize(ref data3, data3.Length + data4.Length);
            Array.Copy(data4, 0, data3, originalSize, data4.Length);

            sut.FilterAndStoreData(data3, data3.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_FilterAndStoreData_MultipleMixedIp4Ip6Packets()
        {
            byte[] data1 = ConstructIP6Packet(
                6, 0, 0,
                0, 0, 0, 0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );
            byte[] data2 = ConstructIP6Packet(
                6, 0, 0,
                0, 0, 0, 0,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );
            int originalSize = data1.Length;
            Array.Resize(ref data1, data1.Length + data2.Length);
            Array.Copy(data2, 0, data1, originalSize, data2.Length);

            IPDecoder sut = new IPDecoder(1, 1, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_GetNextIPPayload_SinglePacket()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);
            Assert.IsTrue(sut.Fragments.Count == 1);
            Assert.IsTrue(TestInfrastructure.Listener.Messages.Count == 0);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_GetNextIPPayload_SinglePacketHWAccel()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                true
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data, data.Length);
            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        /// <summary>
        /// Tests when two IP fragments are there but out of order, should return both - in any order.
        /// </summary>
        [TestMethod]
        public void IPDecoder_GetNextIPPayload_OutOfOrderPackets()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1112, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 1111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            int originalSize = data1.Length;
            Array.Resize(ref data1, data1.Length + data2.Length);
            Array.Copy(data2, 0, data1, originalSize, data2.Length);
            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);
            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 11, ret[i]);
            ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_GetNextIPPayload_TwoPacketFragment()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1111, (byte)IPFragment.MF, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 1111, 0, 16, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 17, 18, 19, 20, 21, 22, 23, 24 }
            );

            int originalSize = data1.Length;
            Array.Resize(ref data1, data1.Length + data2.Length);
            Array.Copy(data2, 0, data1, originalSize, data2.Length);
            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);
            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(24, ret?.Length);
            for (int i = 0; i < 24; i++)
                Assert.AreEqual(i + 1, ret[i]);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void IPDecoder_GetNextIPPayload_SingleFragmentTimeout()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1111, (byte)IPFragment.MF, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 2111, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);
            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(null, ret);

            sut.FilterAndStoreData(data2, data2.Length);
            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 11, ret[i]);

            Assert.AreEqual(0, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        /// <summary>
        /// this tests that a single IP fragment and a single packet will return just the packet.
        /// </summary>
        [TestMethod]
        public void IPDecoder_GetNextIPPayload_OneFragmentOnePacket()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1111, (byte)IPFragment.MF, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 1112, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);
            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(null, ret);

            sut.FilterAndStoreData(data2, data2.Length);
            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 11, ret[i]);

            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        /// <summary>
        /// Tests the use of an ip4 packet with header options
        /// </summary>
        //[TestMethod]
        public void IPDecoder_IP4HeaderOptions()
        {
            Assert.Fail();
        }

        /// <summary>
        /// this tests that two IP fragments surrounding a single packet will return just the packet.
        /// </summary>
        [TestMethod]
        public void IPDecoder_GetNextIPPayload_OneFragmentTwoPackets()
        {
            uint sourceIP = ConversionUtility.IPStringToUint("1.2.3.4");
            uint destinationIP = ConversionUtility.IPStringToUint("2.3.4.5");

            byte[] data1 = ConstructIP4Packet(
                4, 1111, (byte)IPFragment.MF, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }
            );

            byte[] data2 = ConstructIP4Packet(
                4, 1112, 0, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }
            );

            byte[] data3 = ConstructIP4Packet(
                4, 1113, (byte)IPFragment.MF, 0, IPProtocol.TCP,
                sourceIP,
                destinationIP,
                new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }
            );
            IPDecoder sut = new IPDecoder(sourceIP, destinationIP, IPProtocol.TCP);

            sut.FilterAndStoreData(data1, data1.Length);
            Assert.AreEqual(1, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextIPPayload();
            Assert.AreEqual(null, ret);

            sut.FilterAndStoreData(data2, data2.Length);
            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            sut.FilterAndStoreData(data3, data3.Length);
            Assert.AreEqual(3, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextIPPayload();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 11, ret[i]);

            Assert.AreEqual(2, sut.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

        }

        /// <summary>
        /// This tests decoding a real-world fragmented ICMP ping response.
        /// </summary>
        [TestMethod]
        public void IPDecoder_DecodeFragmentedICMP()
        {
            byte[] packet1 = ConversionUtility.HexStringToByteArray("450005DC3201200080010000C0A8010617C01E2E0800AE740001003D6162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F70717273747576776162636465666768696A6B6C6D6E6F7071727374757677");
            byte[] packet2 = ConversionUtility.HexStringToByteArray("4500001C320100B980010000C0A8010617C01E2E6162636465666768");

            IPDecoder decoder = new IPDecoder(
                ConversionUtility.IPStringToUint("192.168.1.6"),
                ConversionUtility.IPStringToUint("23.192.30.46"),
                IPProtocol.ICMP);

            decoder.FilterAndStoreData(packet1, packet1.Length);
            decoder.FilterAndStoreData(packet2, packet2.Length);

            Assert.AreEqual(2, decoder.Fragments.Count);

            byte[] result = decoder.GetNextIPPayload();
            Assert.AreEqual(1488, result.Length); // todo: is this correct?

            Assert.AreEqual(0, decoder.Fragments.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        private unsafe byte[] ConstructIP4Packet(
            byte ipversion,
            ushort id,
            byte flags,
            ushort fragmentoffset,
            IPProtocol protocol,
            uint source_address,
            uint destination_address,
            byte[] data,
            bool bHardware = false
            )
        {
            int size = sizeof(IPv4Header) + (data?.Length ?? 0);
            byte[] ret = new byte[size];

            IPv4Header header = new IPv4Header();
            header.version_ihl = (byte)((ipversion << 4) + (sizeof(IPv4Header) / 4));
            header.tos_ecn = 0;
            if (!bHardware)
                header.packet_length = ConversionUtility.htons((ushort)ret.Length);
            header.identification = ConversionUtility.htons(id);
            //header.flags_fragmentoffset = (ushort)(flags + Utility.htons((ushort)(fragmentoffset << 3)));
            header.flags_fragmentoffset = (ushort)((flags << 4) + (ConversionUtility.htons(fragmentoffset) >> 3));
            header.ttl = 0;
            header.protocol = protocol;
            header.checksum = 0;
            header.ip_srcaddr = source_address; // not sure why, these are not coming across the wire as net order?
            header.ip_destaddr = destination_address; // not sure why, these are not coming across the wire as net order?

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(sizeof(IPv4Header));
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, ret, 0, sizeof(IPv4Header));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            if ((data?.Length ?? 0) > 0)
                Array.Copy(data, 0, ret, sizeof(IPv4Header), data.Length);

            return ret;
        }

        private unsafe byte[] ConstructIP6Packet(
            byte ipversion,
            ushort trafficclass,
            uint flowlabel,
            ulong source_address1,
            ulong source_address2,
            ulong destination_address1,
            ulong destination_address2,
            byte[] data
            )
        {
            int size = data?.Length ?? 0;
            if (size % 8 != 0)
                size += 8 - (size % 8);
            byte[] ret = new byte[size + sizeof(IPv6Header)];

            IPv6Header header = new IPv6Header();
            ushort temp = ConversionUtility.htons((ushort)((ipversion << 4) + trafficclass));
            header.version_ltc = (byte)(temp >> 8);
            header.htc_lfl = (byte)((temp & 0xff) + (flowlabel >> 8));
            header.flow_label = (ushort)(flowlabel & 0xff);
            header.payload_length = ConversionUtility.htons((ushort)(size / 8));
            header.next_header = 0;
            header.hop_limit = 60;
            header.source_address1 = ConversionUtility.htonq(source_address1);
            header.source_address2 = ConversionUtility.htonq(source_address2);
            header.dest_address1 = ConversionUtility.htonq(destination_address1);
            header.dest_address2 = ConversionUtility.htonq(destination_address2);

            // todo: what about custom headers?

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(sizeof(IPv6Header));
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, ret, 0, sizeof(IPv6Header));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            if ((data?.Length ?? 0) > 0)
                Array.Copy(data, 0, ret, sizeof(IPv6Header), data.Length);

            return ret;
        }
    }
}

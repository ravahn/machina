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
    public class TCPDecoderTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }

        [TestMethod]
        public void TCPDecoder_StoreData_OnePacket()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                (byte)TCPOptions.PSH,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet);

            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_FirstPacket()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                (byte)TCPOptions.PSH,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
            byte[] ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }


        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_OnePacketNoPSH()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
            byte[] ret = sut.GetNextTCPDatagram();
            Assert.IsNotNull(ret);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_TwoPackets_TwoPSH()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                (byte)TCPOptions.PSH,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            byte[] packet2 = ConstructTCPPacket(
                source_port, dest_port,
                1121,
                1131,
                (byte)TCPOptions.PSH,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet);
            sut.FilterAndStoreData(packet2);
            Assert.AreEqual(2, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 11, ret[i]);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_TwoPackets_OnePSH()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            byte[] packet2 = ConstructTCPPacket(
                source_port, dest_port,
                1121,
                1131,
                (byte)TCPOptions.PSH,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet);
            sut.FilterAndStoreData(packet2);
            Assert.AreEqual(2, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(20, ret.Length);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_TwoPacketsOutOfOrder()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet0 = ConstructTCPPacket(
                source_port, dest_port,
                1101,
                1111,
                0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            byte[] packet1 = ConstructTCPPacket(
                source_port, dest_port,
                1121,
                1131,
                (byte)TCPOptions.PSH,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });


            byte[] packet2 = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                0,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet0);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
            byte[] ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret?.Length);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            sut.FilterAndStoreData(packet1);
            ret = sut.GetNextTCPDatagram();
            Assert.IsNull(ret);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            sut.FilterAndStoreData(packet2);
            Assert.AreEqual(2, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(20, ret?.Length);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_TwoPacketsMissingOne()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet1 = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                (byte)TCPOptions.PSH,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            byte[] packet2 = ConstructTCPPacket(
                source_port, dest_port,
                1131,
                1141,
                (byte)TCPOptions.PSH,
                new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet1);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
            byte[] ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret?.Length);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            sut.FilterAndStoreData(packet2);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextTCPDatagram();
            Assert.IsNull(ret);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
        [TestMethod]
        public void TCPDecoder_GetNextTCPPacket_SynAndPacket()
        {
            ushort source_port = 111;
            ushort dest_port = 222;

            byte[] packet1 = ConstructTCPPacket(
                source_port, dest_port,
                1110, // note: SYN packet will reset position to identifier+1.
                1121,
                (byte)TCPOptions.SYN,
                new byte[] { });

            byte[] packet2 = ConstructTCPPacket(
                source_port, dest_port,
                1111,
                1121,
                (byte)TCPOptions.PSH,
                new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });



            TCPDecoder sut = new TCPDecoder(source_port, dest_port);

            sut.FilterAndStoreData(packet1);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            byte[] ret = sut.GetNextTCPDatagram();
            Assert.IsNull(ret);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            sut.FilterAndStoreData(packet2);
            Assert.AreEqual(1, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);

            ret = sut.GetNextTCPDatagram();
            Assert.AreEqual(10, ret?.Length);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, ret[i]);
            Assert.AreEqual(0, sut.Packets.Count);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        private unsafe byte[] ConstructTCPPacket(
            ushort source_port,
            ushort dest_port,
            uint seq_no,
            uint ack_no,
            byte flags,
            byte[] data
            )
        {
            byte[] ret = new byte[data.Length + sizeof(TCPHeader)];
            TCPHeader header = new TCPHeader();
            header.source_port = ConversionUtility.htons(source_port);
            header.destination_port = ConversionUtility.htons(dest_port);
            header.sequence_number = ConversionUtility.htonl(seq_no);
            header.ack_number = ConversionUtility.htonl(ack_no);
            header.dataoffset_ns = (byte)((sizeof(TCPHeader) / 4) << 4);
            header.flags = flags;
            header.windowsize = 0;
            header.checksum = 0;
            header.urgent = 0;

            // todo: what about custom options?

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(sizeof(TCPHeader));
                Marshal.StructureToPtr(header, ptr, true);
                Marshal.Copy(ptr, ret, 0, sizeof(TCPHeader));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            if ((data?.Length ?? 0) > 0)
                Array.Copy(data, 0, ret, sizeof(TCPHeader), data.Length);

            return ret;
        }
    }
}


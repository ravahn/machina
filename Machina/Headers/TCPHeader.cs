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
using Machina.Infrastructure;

namespace Machina.Headers
{
    [Flags]
    public enum TCPOptions
    {
        FIN = 0x01,
        SYN = 0x02,
        RST = 0x04,
        PSH = 0x08,
        ACK = 0x10,
        URG = 0x20,
        ECE = 0x40,
        CWR = 0x80,
        NS = 0x100
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct TCPHeader
    {
        [FieldOffset(0)]
        public ushort source_port;
        [FieldOffset(2)]
        public ushort destination_port;
        [FieldOffset(4)]
        public uint sequence_number;
        [FieldOffset(8)]
        public uint ack_number;
        [FieldOffset(12)]
        public byte dataoffset_ns;
        [FieldOffset(13)]
        public byte flags;
        [FieldOffset(14)]
        public ushort windowsize;
        [FieldOffset(16)]
        public ushort checksum;
        [FieldOffset(18)]
        public ushort urgent;

        public ushort SourcePort => ConversionUtility.ntohs(source_port);
        public ushort DestinationPort => ConversionUtility.ntohs(destination_port);
        public uint SequenceNumber => ConversionUtility.ntohl(sequence_number);
        public byte DataOffset => (byte)((dataoffset_ns >> 4) * 4);
        public uint AckNumber => ConversionUtility.ntohl(ack_number);
        public uint WindowSize => ConversionUtility.ntohs(windowsize);
        public uint Checksum => ConversionUtility.ntohs(checksum);
        public uint Urgent => ConversionUtility.ntohs(urgent);
    }
}

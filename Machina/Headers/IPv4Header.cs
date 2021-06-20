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

using System.Runtime.InteropServices;
using Machina.Infrastructure;

namespace Machina.Headers
{
    public enum IPProtocol : byte
    {
        ICMP = 1,
        TCP = 6,
        UDP = 17
    };

    public enum IPFragment : byte
    {
        Reserved = 1, // must be zero
        MF = 2, // Must Fragment
        DF = 4 // Dont Fragment
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct IPv4Header
    {
        [FieldOffset(0)]
        public byte version_ihl; // low word = Version, high word = header length
        [FieldOffset(1)]
        public byte tos_ecn; // low 6 bits = ToS, upper 2 bits = congestion notification
        [FieldOffset(2)]
        public ushort packet_length; // total packet length
        [FieldOffset(4)]
        public ushort identification; // 2 bytes packet identification
        [FieldOffset(6)]
        public ushort flags_fragmentoffset; // low 3 bits = flags, upper 13 bytes = frament offset
        [FieldOffset(8)]
        public byte ttl; // time to live
        [FieldOffset(9)]
        public IPProtocol protocol; // tcp, udp, etc
        [FieldOffset(10)]
        public ushort checksum; // 2 bytes checksum
        [FieldOffset(12)]
        public uint ip_srcaddr; // source IP address in network order
        [FieldOffset(16)]
        public uint ip_destaddr; // destination IP address in network order

        /// <summary>
        /// IP version number 
        /// </summary>
        public byte Version => (byte)(version_ihl >> 4);

        /// <summary>
        /// IP Header length
        /// </summary>
        public byte HeaderLength => (byte)((version_ihl & 0x0f) * 4);

        /// <summary>
        /// total packet length, including data payload, in host byte order
        /// </summary>
        public ushort Length => ConversionUtility.ntohs(packet_length);

        /// <summary>
        /// IP packet identifier in host byte order
        /// </summary>
        public ushort Id => ConversionUtility.ntohs(identification);

        /// <summary>
        /// IP flags
        /// </summary>
        public byte Flags => (byte)((flags_fragmentoffset & 0xe0) >> 4);

        /// <summary>
        /// IP Fragment offset in host byte order
        /// </summary>
        public ushort FragmentOffset => (ushort)(ConversionUtility.ntohs(flags_fragmentoffset) << 3);

    }
}

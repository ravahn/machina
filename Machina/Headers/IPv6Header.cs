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
    [StructLayout(LayoutKind.Explicit)]
    public struct IPv6Header
    {
        [FieldOffset(0)]
        public byte version_ltc; // low word = Version, high word = portion of traffic class 
        [FieldOffset(1)]
        public byte htc_lfl; // low 4 bits = portion of traffic class, upper 4 bits = part of flow label
        [FieldOffset(2)]
        public ushort flow_label; // remainder of flow label
        [FieldOffset(4)]
        public ushort payload_length;
        [FieldOffset(6)]
        public byte next_header; // type of next header
        [FieldOffset(7)]
        public byte hop_limit;
        [FieldOffset(8)]
        public ulong source_address1;
        [FieldOffset(16)]
        public ulong source_address2;
        [FieldOffset(24)]
        public ulong dest_address1;
        [FieldOffset(32)]
        public ulong dest_address2;

        public const byte MinSize = 40;

        public byte Version => (byte)(version_ltc >> 4);

        public ushort PayloadLength => ConversionUtility.ntohs(payload_length);
    }
}

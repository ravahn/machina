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

namespace Machina.FFXIV.Headers
{
    public enum CompressionType : ushort
    {
        None = 0x00,
        Zlib = 0x01,
        Oodle = 0x02
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Server_BundleHeader
    {
        [FieldOffset(0)]
        public uint magic0;
        [FieldOffset(4)]
        public uint magic1;
        [FieldOffset(8)]
        public uint magic2;
        [FieldOffset(12)]
        public uint magic3;
        [FieldOffset(16)]
        private readonly ulong _epoch;
        [FieldOffset(24)]
        public uint length;
        [FieldOffset(28)]
        public ushort channel;
        [FieldOffset(30)]
        public ushort message_count;
        [FieldOffset(32)]
        public byte version;
        [FieldOffset(33)]
        public CompressionType compression;
        [FieldOffset(34)]
        public ushort unknown1;
        [FieldOffset(36)]
        public uint uncompressed_length;

        //public const int MinSize = 40;

        public ulong epoch =>
            (((ulong)ConversionUtility.ntohl((uint)(int)(_epoch & 0xFFFFFFFF))) << 32) +
                     ConversionUtility.ntohl((uint)(int)(_epoch >> 32));
    }
}

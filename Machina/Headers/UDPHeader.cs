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

namespace Machina.Headers
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UDPHeader
    {
        [FieldOffset(0)]
        public ushort source_port;
        [FieldOffset(2)]
        public ushort destination_port;
        [FieldOffset(4)]
        public ushort length;
        [FieldOffset(6)]
        public ushort checksum;
    }
}

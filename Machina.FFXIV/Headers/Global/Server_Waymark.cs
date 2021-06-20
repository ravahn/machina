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

namespace Machina.FFXIV.Headers
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_Waymark
    {
        public enum WaymarkStatus : byte
        {
            Off = 0,
            On = 1
        };
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public WaymarkType Waymark;
        public WaymarkStatus Status;
        public ushort unknown;
        public int PosX;
        public int PosZ;// To calculate 'float' coords from these you cast them to float and then divide by 1000.0
        public int PosY;
    }
}

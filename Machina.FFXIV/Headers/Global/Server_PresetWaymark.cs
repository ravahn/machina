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
    // Thanks to Discord user Wintermute for decoding this
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_PresetWaymark
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public WaymarkType WaymarkType;
        public byte Unknown1;
        public short Unknown2;
        public fixed int PosX[8];// Xints[0] has X of waymark A, Xints[1] X of B, etc.
        public fixed int PosZ[8];// To calculate 'float' coords from these you cast them to float and then divide by 1000.0
        public fixed int PosY[8];
    }
}

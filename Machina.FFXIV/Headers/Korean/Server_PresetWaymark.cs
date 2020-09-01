// Machina.FFXIV ~ Server_ActorGauge.cs
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
using System;
using System.Runtime.InteropServices;

namespace Machina.FFXIV.Headers.Korean
{
    // Thanks to Discord user Wintermute for decoding this
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_PresetWaymark
    {
        public enum WaymarkStatusEnum : UInt32
        {
            A = 0x1,
            B = 0x2,
            C = 0x4,
            D = 0x8,
            One = 0x10,
            Two = 0x20,
            Three = 0x40,
            Four = 0x80,
        };

        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public WaymarkStatusEnum WaymarkStatus;
        public fixed UInt32 PosX[8];// Xints[0] has X of waymark A, Xints[1] X of B, etc.
        public fixed UInt32 PosY[8];// To calculate 'float' coords from these you cast them to float and then divide by 1000.0
        public fixed UInt32 PosZ[8];
    }
}

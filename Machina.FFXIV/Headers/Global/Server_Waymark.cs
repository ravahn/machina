// Machina.FFXIV ~ Server_Waymark.cs
// 
// Copyright © 2020 Ravahn - All Rights Reserved
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

namespace Machina.FFXIV.Headers
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_Waymark
    {
        public enum WaymarkEnum : byte
        {
            A = 0x0,
            B = 0x1,
            C = 0x2,
            D = 0x3,
            One = 0x4,
            Two = 0x5,
            Three = 0x6,
            Four = 0x7,
        };
        public enum StatusEnum : byte
        {
            Off = 0,
            On = 1
        };
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public WaymarkEnum Waymark;
        public StatusEnum Status;
        public UInt16 unknown;
        public Int32 PosX;
        public Int32 PosZ;// To calculate 'float' coords from these you cast them to float and then divide by 1000.0
        public Int32 PosY;
    }
}

// Machina.FFXIV ~ Server_UpdateHpMpTp.cs
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_UpdateHpMpTp
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public UInt32 CurrentHp;
        public UInt16 CurrentMp;
        public UInt16 CurrentTp;
        public UInt16 unknown1;
        public UInt16 unknown2;
        public UInt16 unknown3;
        public UInt16 unknown4;
    }
}

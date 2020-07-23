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

namespace Machina.FFXIV.Headers.Chinese
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorGauge
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public UInt32 param1; // first byte is classjobid
        public UInt32 param2;
        public UInt32 param3;
        public UInt32 param4;
    }
}

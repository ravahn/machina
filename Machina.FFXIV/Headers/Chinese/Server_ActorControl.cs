// Machina.FFXIV ~ Server_ActorControl.cs
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
    public enum Server_ActorControlCategory : UInt16
    {
        HoT_DoT = 0x17,
        CancelAbility = 0x0f,
        Death = 0x06,
        TargetIcon = 0x22,
        Tether = 0x23,
        GainEffect = 0x14,
        LoseEffect = 0x15,
        UpdateEffect = 0x16,
        Targetable = 0x36,
        DirectorUpdate = 0x6d,
        LimitBreak = 0x1f9
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControl142
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public UInt16 padding;
        public UInt32 param1;
        public UInt32 param2;
        public UInt32 param3;
        public UInt32 param4;
        public UInt32 padding1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControl143
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public UInt16 padding;
        public UInt32 param1;
        public UInt32 param2;
        public UInt32 param3;
        public UInt32 param4;
        public UInt32 param5;
        public UInt32 param6;
        public UInt32 padding1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControl144
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public UInt16 padding;
        public UInt32 param1;
        public UInt32 param2;
        public UInt32 param3;
        public UInt32 param4;
        public UInt32 padding1;
        public UInt32 TargetID;
        public UInt32 padding2;
    }
}

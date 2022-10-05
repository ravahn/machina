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

namespace Machina.FFXIV.Headers.Korean
{
    public enum Server_ActorControlCategory : ushort
    {
        DoT = 0x17,
        HoT = 0x603,
        CancelAbility = 0x0f,
        Death = 0x06,
        TargetIcon = 0x22,
        Tether = 0x23,
        GainEffect = 0x14,
        LoseEffect = 0x15,
        UpdateEffect = 0x16,
        Targetable = 0x36,
        DirectorUpdate = 0x6d,
        SetTargetSign = 0x1f6,
        LimitBreak = 0x1f9
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControl
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public ushort padding;
        public uint param1;
        public uint param2;
        public uint param3;
        public uint param4;
        public uint padding1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControlSelf
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public ushort padding;
        public uint param1;
        public uint param2;
        public uint param3;
        public uint param4;
        public uint param5;
        public uint param6;
        public uint padding1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorControlTarget
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public Server_ActorControlCategory category;
        public ushort padding;
        public uint param1;
        public uint param2;
        public uint param3;
        public uint param4;
        public uint padding1;
        public uint TargetID;
        public uint padding2;
    }
}

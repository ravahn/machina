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

namespace Machina.FFXIV.Headers.Chinese
{
    public enum Server_ActionEffectDisplayType : byte
    {
        HideActionName = 0,
        ShowActionName = 1,
        ShowItemName = 2,
        MountName = 0x0d
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffectHeader
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public uint animationTargetId;  // who the animation targets
        public uint unknown;
        public uint actionId; // what the casting player casts, shown in battle log / ui
        public uint globalEffectCounter;
        public float animationLockTime;
        public uint SomeTargetID;
        public ushort hiddenAnimation; // 0 = show animation, otherwise hide animation.
        public ushort rotation;
        public ushort actionAnimationId;
        public byte variation; // animation
        public Server_ActionEffectDisplayType effectDisplayType; // is this also item id / mount id?
        public byte unknown20;
        public byte effectCount;
        public ushort padding21;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect1
    {
        public Server_ActionEffectHeader Header;
        public uint padding1;
        public ushort padding2;
        public fixed uint Effects[16];
        public ushort padding3;
        public uint padding4;
        public fixed ulong TargetID[1];
        public uint padding5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect8
    {
        public Server_ActionEffectHeader Header;
        public uint padding1;
        public ushort padding2;
        public fixed uint Effects[128];
        public ushort padding3;
        public uint padding4;
        public fixed ulong TargetID[8];
        public uint effectflags1;
        public ushort effectflags2;
        public ushort padding5;
        public uint padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect16
    {
        public Server_ActionEffectHeader Header;
        public uint padding1;
        public ushort padding2;
        public fixed uint Effects[256];
        public ushort padding3;
        public uint padding4;
        public fixed ulong TargetID[16];
        public uint effectflags1;
        public ushort effectflags2;
        public ushort padding5;
        public uint padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect24
    {
        public Server_ActionEffectHeader Header;
        public uint padding1;
        public ushort padding2;
        public fixed uint Effects[384];
        public ushort padding3;
        public uint padding4;
        public fixed ulong TargetID[24];
        public uint effectflags1;
        public ushort effectflags2;
        public ushort padding5;
        public uint padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect32
    {
        public Server_ActionEffectHeader Header;
        public uint padding1;
        public ushort padding2;
        public fixed uint Effects[512];
        public ushort padding3;
        public uint padding4;
        public fixed ulong TargetID[32];
        public uint effectflags1;
        public ushort effectflags2;
        public ushort padding5;
        public uint padding6;
    }
}

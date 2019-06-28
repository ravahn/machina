// Machina.FFXIV ~ Server_ActionEffect.cs
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

namespace Machina.FFXIV.Headers
{
    public enum Server_ActionEffectDisplayType : byte
    {
        HideActionName = 0,
        ShowActionName = 1,
        ShowItemName = 2,
        MountName = 0x0d
    };

    /*
    public struct EffectEntry
    {
        byte effectType;
        byte hitSeverity;
        byte param;
        sbyte bonusPercent;
        byte valueMultiplier;
        byte flag;
        UInt16 value;
    }*/

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffectHeader
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public UInt32 animationTargetId;  // who the animation targets
        public UInt32 unknown;
        public UInt32 actionId; // what the casting player casts, shown in battle log / ui
        public UInt32 globalEffectCounter;
        public float animationLockTime;
        public UInt32 SomeTargetID;
        public UInt16 hiddenAnimation; // 0 = show animation, otherwise hide animation.
        public UInt16 rotation;
        public UInt16 actionAnimationId;
        public byte variation; // animation
        public Server_ActionEffectDisplayType effectDisplayType; // is this also item id / mount id?
        public byte unknown20;
        public byte effectCount;
        public UInt16 padding21;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect1
    {
        public Server_ActionEffectHeader Header;
        public UInt32 padding1;
        public UInt16 padding2;
        public fixed UInt32 Effects[16];
        public UInt16 padding3;
        public UInt32 padding4;
        public fixed UInt64 TargetID[1];
        public UInt32 padding5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect8
    {
        public Server_ActionEffectHeader Header;
        public UInt32 padding1;
        public UInt16 padding2;
        public fixed UInt32 Effects[128];
        public UInt16 padding3;
        public UInt32 padding4;
        public fixed UInt64 TargetID[8];
        public UInt32 effectflags1;
        public UInt16 effectflags2;
        public UInt16 padding5;
        public UInt32 padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect16
    {
        public Server_ActionEffectHeader Header;
        public UInt32 padding1;
        public UInt16 padding2;
        public fixed UInt32 Effects[256];
        public UInt16 padding3;
        public UInt32 padding4;
        public fixed UInt64 TargetID[16];
        public UInt32 effectflags1;
        public UInt16 effectflags2;
        public UInt16 padding5;
        public UInt32 padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect24
    {
        public Server_ActionEffectHeader Header;
        public UInt32 padding1;
        public UInt16 padding2;
        public fixed UInt32 Effects[384];
        public UInt16 padding3;
        public UInt32 padding4;
        public fixed UInt64 TargetID[24];
        public UInt32 effectflags1;
        public UInt16 effectflags2;
        public UInt16 padding5;
        public UInt32 padding6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_ActionEffect32
    {
        public Server_ActionEffectHeader Header;
        public UInt32 padding1;
        public UInt16 padding2;
        public fixed UInt32 Effects[512];
        public UInt16 padding3;
        public UInt32 padding4;
        public fixed UInt64 TargetID[32];
        public UInt32 effectflags1;
        public UInt16 effectflags2;
        public UInt16 padding5;
        public UInt32 padding6;
    }
}

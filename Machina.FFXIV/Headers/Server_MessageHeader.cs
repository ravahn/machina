// Machina.FFXIV ~ Server_MessageHeader.cs
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
    /// <summary>
    /// Enumerates the known FFXIV server message types.  Note that some names were adopted from the Sapphire project
    /// </summary>
    public enum Server_MessageType : ushort
    {
        StatusEffectList = 0x0399,
        BossStatusEffectList = 0x00e6,//to fix
        Ability1 = 0x0165,
        Ability8 = 0x00e9,
        Ability16 = 0x007f,
        Ability24 = 0x0299, 
        Ability32 = 0x01df,
        ActorCast = 0x028e,
        AddStatusEffect = 0x00b9,
        ActorControl142 = 0x008d,
        ActorControl143 = 0x00eb,
        ActorControl144 = 0x01f5,
        UpdateHpMpTp = 0x012d,
        PlayerSpawn = 0x0243,
        NpcSpawn = 0x021b,
        NpcSpawn2 = 0x0137,
        ActorMove = 0x00dd,
        ActorSetPos = 0x0092,
        ActorGauge = 0x037f // to fix
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Server_MessageHeader
    {
        [FieldOffset(0)]
        public uint MessageLength;
        [FieldOffset(4)]
        public uint ActorID;
        [FieldOffset(8)]
        public uint LoginUserID;
        [FieldOffset(12)]
        public uint Unknown1;
        [FieldOffset(16)]
        public ushort Unknown2;
        [FieldOffset(18)]
        public Server_MessageType MessageType;
        [FieldOffset(20)]
        public uint Unknown3;
        [FieldOffset(24)]
        public uint Seconds;
        [FieldOffset(28)]
        public uint Unknown4;
    }    
}
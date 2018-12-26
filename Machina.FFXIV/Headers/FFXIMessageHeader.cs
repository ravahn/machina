// Machina.FFXIV ~ FFXIVMessageHeader.cs
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

using System.Runtime.InteropServices;

namespace Machina.FFXIV
{
    /// <summary>
    /// Enumerates the known FFXIV server message types.  Note that some names were adopted from the Sapphire project
    /// </summary>
    public enum ServerMessageType : ushort
    {
        StatusEffectList = 0x0149,
        Ability1 = 0x014c,
        Ability8 = 0x014f,
        Ability16 = 0x0150,
        Ability24 = 0x0151, 
        Ability32 = 0x0152,
        ActorCast = 0x0174,
        AddStatusEffect = 0x0141,
        ActorControl142 = 0x0142,
        ActorControl143 = 0x0143,
        ActorControl144 = 0x0144,
        ActorGauge = 0x0292
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FFXIVMessageHeader
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
        public ushort MessageType;
        [FieldOffset(20)]
        public uint Unknown3;
        [FieldOffset(24)]
        public uint Seconds;
        [FieldOffset(28)]
        public uint Unknown4;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MessageActorControl142
    {
        [FieldOffset(0)]
        public FFXIVMessageHeader Header;
        [FieldOffset(32)]
        public ushort Unknown1; // main type - 15 = lost buff?
        [FieldOffset(34)]
        public ushort Unknown2;
        [FieldOffset(36)]
        public ushort Data1; // buff id
        [FieldOffset(38)]
        public ushort Data2; // buff extra
        [FieldOffset(40)]
        public ushort Unknown3;
        [FieldOffset(44)]
        public ushort Unknown4;
        [FieldOffset(48)]
        public ushort Unknown5;
        [FieldOffset(52)]
        public ushort Unknown6;
    }


    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MessageStatusEffectList
    {
        [FieldOffset(0)]
        public FFXIVMessageHeader Header;
        [FieldOffset(32)]
        public byte JobID;
        [FieldOffset(33)]
        public byte Level;
        [FieldOffset(34)]
        public byte Level2; // seems like duplicate of level?
        [FieldOffset(35)]
        public byte Unknown; // zeros?
        [FieldOffset(36)]
        public uint CurrentHP;
        [FieldOffset(40)]
        public uint MaxHP;
        [FieldOffset(44)]
        public ushort CurrentMP;
        [FieldOffset(46)]
        public ushort MaxMP;
        [FieldOffset(48)]
        public ushort CurrentTP;
        [FieldOffset(50)]
        public ushort EffectCount;
        [FieldOffset(52)]
        public StatusEffect* StatusEffects; // note: up to 30 status effects only.
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct StatusEffect
    {
        [FieldOffset(0)]
        public ushort BuffID;
        [FieldOffset(2)]
        public ushort BuffExtra;
        [FieldOffset(4)]
        public short Timer;
        [FieldOffset(8)]
        public short ActorID;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MessageAddStatusEffect
    {
        [FieldOffset(0)]
        public FFXIVMessageHeader Header;
        [FieldOffset(32)]
        public uint Unknown1; // Could this be an action identifier?
        [FieldOffset(36)]
        public uint ActorID; // Seems to be the id of the player/mob triggering the update
        [FieldOffset(40)]
        public byte Unknown2; 
        [FieldOffset(41)]
        public byte JobID;
        [FieldOffset(42)]
        public byte Unknown3;
        [FieldOffset(43)]
        public byte Unknown4; 
        [FieldOffset(44)]
        public uint CurrentHP;
        [FieldOffset(48)]
        public ushort CurrentMP;
        [FieldOffset(50)]
        public ushort CurrentTP;
        [FieldOffset(52)]
        public uint MaxHP;
        [FieldOffset(56)]
        public ushort MaxMP;
        [FieldOffset(58)]
        public byte EffectCount;
        [FieldOffset(59)]
        public byte Unknown9; // C1, 43, 3f, C0, ???
        [FieldOffset(60)]
        public StatusEffect4* effects; // up to 4 of these, 16 bytes each.
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct StatusEffect4
    {
        [FieldOffset(0)]
        public byte EffectPosition; // position the status effect should be added
        [FieldOffset(1)]
        public byte Unknown10; // 0A, 23, 00, 5D, etc - lots of values
        [FieldOffset(2)]
        public ushort BuffID;
        [FieldOffset(4)]
        public uint Unknown11; // 4 bytes may be extra info, but 4 seem unused?
        [FieldOffset(8)]
        public short Timer; // Seems to be default value for most of these.
        [FieldOffset(12)]
        public uint ActorID2;
    }
}
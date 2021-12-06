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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_EffectResultEntry
    {
        public byte EffectIndex;
        public byte unknown1;
        public ushort EffectID;
        public ushort unknown2;
        public ushort unknown3;
        public float duration;
        public uint SourceActorID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_EffectResult
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public uint RelatedActionSequence;
        public uint ActorID;
        public uint CurrentHP;
        public uint MaxHP;
        public ushort CurrentMP;
        public ushort Unknown3;
        //public UInt16 MaxMP;
        //public UInt16 Unknown4;
        public byte DamageShield;
        public byte EffectCount;
        public ushort Unknown6;
        public fixed byte Effects[4 * 4 * 4];
    }
}

// Machina.FFXIV ~ Server_AddStatusEffect.cs
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_StatusEffectAddEntry
    {
        public byte EffectIndex;
        public byte unknown1;
        public UInt16 EffectID;
        public UInt16 unknown2;
        public UInt16 unknown3;
        public float duration;
        public UInt32 SourceActorID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_AddStatusEffect
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public UInt32 RelatedActionSequence;
        public UInt32 ActorID;
        public UInt32 CurrentHP;
        public UInt32 MaxHP;
        public UInt16 CurrentMP;
        public UInt16 Unknown3;
//        public UInt16 MaxMP;
  //      public UInt16 Unknown4;
        public byte DamageShield;
        public byte EffectCount;
        public UInt16 Unknown6;
        public fixed byte Effects[4 * 4 * 4];
    }
}

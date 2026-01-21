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


namespace Machina.FFXIV.Headers
{
    public enum Server_SkillType : byte
    {
        None = 0,
        Spell = 1, // Ability sheet
        Item = 2, // Item sheet
        KeyItem = 3, // EventItem sheet
        Mount = 13, // Mount sheet
        Accessory = 20 // Ornament sheet
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_ActorCast
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS
        public ushort ActionID;
        public Server_SkillType SkillType;
        public byte VfxAnimationDelay; // Delay in 100ms increments for VFX of action
        public uint SheetID; // Sheet row ID, sheet depends on SkillType
        public float CastTime;
        public uint TargetID;
        public ushort Rotation; // Rotation of MessageHeader.ActorID
        public byte Interruptible; // If 1, cast bar shows as flashing/interruptible with Interject
        public byte Unknown1; // Probably padding?
        public uint UnknownID; // Some sort of actor ID, always E0000000 in normal content
        public ushort PosX; // X/Y/Z of target. If TargetID is an actor, this is the actor's position. If TargetID is E0000000, this is the cast target in world coordinates
        public ushort PosY;
        public ushort PosZ;
        public ushort Unknown2; // Padding, always 0000, never referenced in client
    }
}

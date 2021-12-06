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

namespace Machina.FFXIV.Headers
{
    /// <summary>
    /// Enumerates the known FFXIV server message types.  Note that some names were adopted from the Sapphire project
    /// </summary>

    public struct Server_MessageType
    {
        public static readonly Server_MessageType StatusEffectList = Opcodes.OpcodeManager.Instance.CurrentOpcodes["StatusEffectList"];
        public static readonly Server_MessageType StatusEffectList2 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["StatusEffectList2"];
        public static readonly Server_MessageType StatusEffectList3 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["StatusEffectList3"];
        public static readonly Server_MessageType BossStatusEffectList = Opcodes.OpcodeManager.Instance.CurrentOpcodes["BossStatusEffectList"];
        public static readonly Server_MessageType Ability1 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability1"];
        public static readonly Server_MessageType Ability8 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability8"];
        public static readonly Server_MessageType Ability16 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability16"];
        public static readonly Server_MessageType Ability24 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability24"];
        public static readonly Server_MessageType Ability32 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability32"];
        public static readonly Server_MessageType ActorCast = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorCast"];
        public static readonly Server_MessageType EffectResult = Opcodes.OpcodeManager.Instance.CurrentOpcodes["EffectResult"];
        public static readonly Server_MessageType EffectResultBasic = Opcodes.OpcodeManager.Instance.CurrentOpcodes["EffectResultBasic"];
        public static readonly Server_MessageType ActorControl = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControl"];
        public static readonly Server_MessageType ActorControlSelf = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControlSelf"];
        public static readonly Server_MessageType ActorControlTarget = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControlTarget"];
        public static readonly Server_MessageType UpdateHpMpTp = Opcodes.OpcodeManager.Instance.CurrentOpcodes["UpdateHpMpTp"];
        public static readonly Server_MessageType PlayerSpawn = Opcodes.OpcodeManager.Instance.CurrentOpcodes["PlayerSpawn"];
        public static readonly Server_MessageType NpcSpawn = Opcodes.OpcodeManager.Instance.CurrentOpcodes["NpcSpawn"];
        public static readonly Server_MessageType NpcSpawn2 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["NpcSpawn2"];
        public static readonly Server_MessageType ActorMove = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorMove"];
        public static readonly Server_MessageType ActorSetPos = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorSetPos"];
        public static readonly Server_MessageType ActorGauge = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorGauge"];
        public static readonly Server_MessageType PresetWaymark = Opcodes.OpcodeManager.Instance.CurrentOpcodes["PresetWaymark"];
        public static readonly Server_MessageType Waymark = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Waymark"];
        public static readonly Server_MessageType SystemLogMessage = Opcodes.OpcodeManager.Instance.CurrentOpcodes["SystemLogMessage"];

        public ushort InternalValue { get; private set; }

        public override bool Equals(object obj)
        {
            Server_MessageType otherObj = (Server_MessageType)obj;
            return otherObj.InternalValue.Equals(InternalValue);
        }

        public static bool operator ==(Server_MessageType obj1, Server_MessageType obj2)
        {
            return obj1.InternalValue == obj2.InternalValue;
        }
        public static bool operator ==(ushort obj1, Server_MessageType obj2)
        {
            return obj1 == obj2.InternalValue;
        }
        public static bool operator ==(Server_MessageType obj1, ushort obj2)
        {
            return obj1.InternalValue == obj2;
        }

        public override int GetHashCode()
        {
            return InternalValue.GetHashCode();
        }

        public static bool operator !=(Server_MessageType obj1, Server_MessageType ojb2)
        {
            return !(obj1 == ojb2);
        }
        public static bool operator !=(ushort obj1, Server_MessageType ojb2)
        {
            return !(obj1 == ojb2);
        }
        public static bool operator !=(Server_MessageType obj1, ushort ojb2)
        {
            return !(obj1 == ojb2);
        }

        public static implicit operator Server_MessageType(ushort otherType)
        {
            return new Server_MessageType
            {
                InternalValue = otherType
            };
        }

        public static implicit operator ushort(Server_MessageType otherType)
        {
            return otherType.InternalValue;
        }
    }
}

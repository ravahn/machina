using System;

namespace Machina.FFXIV.Headers
{
    /// <summary>
    /// Enumerates the known FFXIV server message types.  Note that some names were adopted from the Sapphire project
    /// </summary>

    public struct Server_MessageType 
    {
        public static readonly Server_MessageType StatusEffectList = Opcodes.OpcodeManager.Instance.CurrentOpcodes["StatusEffectList"];
        public static readonly Server_MessageType BossStatusEffectList = Opcodes.OpcodeManager.Instance.CurrentOpcodes["BossStatusEffectList"];
        public static readonly Server_MessageType Ability1 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability1"];
        public static readonly Server_MessageType Ability8 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability8"]; 
        public static readonly Server_MessageType Ability16 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability16"];
        public static readonly Server_MessageType Ability24 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability24"];
        public static readonly Server_MessageType Ability32 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["Ability32"];
        public static readonly Server_MessageType ActorCast = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorCast"];
        public static readonly Server_MessageType AddStatusEffect = Opcodes.OpcodeManager.Instance.CurrentOpcodes["AddStatusEffect"];
        public static readonly Server_MessageType ActorControl142 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControl142"];
        public static readonly Server_MessageType ActorControl143 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControl143"];
        public static readonly Server_MessageType ActorControl144 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorControl144"];
        public static readonly Server_MessageType UpdateHpMpTp = Opcodes.OpcodeManager.Instance.CurrentOpcodes["UpdateHpMpTp"]; 
        public static readonly Server_MessageType PlayerSpawn = Opcodes.OpcodeManager.Instance.CurrentOpcodes["PlayerSpawn"];
        public static readonly Server_MessageType NpcSpawn = Opcodes.OpcodeManager.Instance.CurrentOpcodes["NpcSpawn"];
        public static readonly Server_MessageType NpcSpawn2 = Opcodes.OpcodeManager.Instance.CurrentOpcodes["NpcSpawn2"];
        public static readonly Server_MessageType ActorMove = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorMove"];
        public static readonly Server_MessageType ActorSetPos = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorSetPos"]; 
        public static readonly Server_MessageType ActorGauge = Opcodes.OpcodeManager.Instance.CurrentOpcodes["ActorGauge"];

        private ushort InternalValue { get; set; }

        public override bool Equals(object obj)
        {
            Server_MessageType otherObj = (Server_MessageType)obj;
            return otherObj.InternalValue.Equals(this.InternalValue);
        }

        public static bool operator ==(Server_MessageType obj1, Server_MessageType obj2)
        {
            return (obj1.InternalValue == obj2.InternalValue);
        }

        public static bool operator !=(Server_MessageType obj1, Server_MessageType ojb2)
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
    }
}
// Machina.FFXIV ~ Server_NpcSpawn.cs
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
    public unsafe struct Server_Npcspawn
    {
        public Server_MessageHeader MessageHeader; // 8 DWORDS

        public UInt32 gimmickId; // needs to be existing in the map, mob will snap to it
        public byte u2b;
        public byte u2ab;
        public byte gmRank;
        public byte u3b;

        public byte aggressionMode; // 1 passive, 2 aggressive
        public byte onlineStatus;
        public byte u3c;
        public byte pose;

        public UInt32 u4;

        public UInt32 targetId;
        public UInt32 unknown;
        public UInt32 u6;
        public UInt32 u7;
        UInt64 mainWeaponModel;
        UInt64 secWeaponModel;
        UInt64 craftToolModel;

        public UInt32 u14;
        public UInt32 u15;
        public UInt32 bNPCBase;
        public UInt32 bNPCName;
        public UInt32 u18;
        public UInt32 u19;
        public UInt32 directorId;
        public UInt32 spawnerId;
        public UInt32 parentActorId;
        public UInt32 hPMax;
        public UInt32 hPCurr;
        public UInt32 displayFlags;
        public UInt16 fateID;
        public UInt16 mPCurr;
        public UInt16 tPCurr;
        public UInt16 mPMax;
        public UInt16 tPMax;
        public UInt16 modelChara;
        public UInt16 rotation;
        public UInt16 activeMinion;
        public byte spawnIndex;
        public byte state;
        public byte persistantEmote;
        public byte modelType;
        public byte subtype;
        public byte voice;
        public UInt16 u25c;
        public byte enemyType;
        public byte level;
        public byte classJob;
        public byte u26d;
        public UInt16 u27a;
        public byte currentMount;
        public byte mountHead;
        public byte mountBody;
        public byte mountFeet;
        public byte mountColor;
        public byte scale;
        public UInt32 u29b;
        public UInt32 u30b;
        public fixed byte Effects[30 * 3 * 4]; 
        public float PosX;
        public float PosY;
        public float PosZ;
        public fixed UInt32 models[10];
        public fixed byte name[32];
        public fixed byte look[26];
        public fixed byte fcTag[6];
        public UInt32 unk30;
        public UInt32 unk31;
        public byte bNPCPartSlot;
        public byte unk32;
        public UInt16 unk33;
        public UInt32 unk34;
    }
}

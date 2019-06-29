// Machina.FFXIV ~ Server_PlayerSpawn.cs
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
    public unsafe struct Server_PlayerSpawn
    {
        /* Needs updating to 5.0 
            public Server_MessageHeader MessageHeader; // 8 DWORDS
            public UInt16 title;
            public UInt16 u1b;
            public UInt16 currentWorldId;
            public UInt16 homeWorldId;
            public byte gmRank;
            public byte u3c;
            public byte u4;
            public byte onlineStatus;
            public byte pose;
            public byte u5a;
            public byte u5b;
            public byte u5c;

            public UInt32 TargetId;
            public UInt32 unknown;
            public UInt32 u6;
            public UInt32 u7;
            public UInt64 mainWeaponMode;
            public UInt64 secWeaponModel;
            public UInt64 craftToolModel;

            public UInt32 u14;
            public UInt32 u15;
            public UInt32 bNPCBase;
            public UInt32 bNPCName;
            public UInt32 u18;
            public UInt32 u19;
            public UInt32 directorId;
            public UInt32 ownerId;
            public UInt32 u22;
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
            public byte persistentEmote;
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
            public UInt16 u30b;
            public UInt16 unknown2; // added 03/23/2019
            public fixed byte Effects[30 * 3 * 4]; 
            public float PosX;
            public float PosY;
            public float PosZ;
            public fixed UInt32 models[10];
            public fixed byte name[32];
            public fixed byte look[26];
            public fixed byte fcTag[6];
            public UInt32 unk30;
        */
    }
}

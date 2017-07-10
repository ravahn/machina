// Machina ~ NetworkConnection.cs
// 
// Copyright © 2007 - 2017 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace Machina
{
    public class NetworkConnection : ServerConnection
    {
        private const int NetworkBufferMax = 0x19000;
        public DateTime LastGoodNetworkPacketTime = DateTime.MinValue;
        public DateTime LastNetworkBufferUpdate = DateTime.Now;
        public byte[] NetworkBuffer = new byte[0x19000];
        public object NetworkBufferLock = new object();
        public Dictionary<uint, NetworkPacket> StalePackets = new Dictionary<uint, NetworkPacket>();
        public object StalePacketsLock = new object();
        public int NetworkBufferPosition { get; set; }
        public uint? NextTCPSequence { get; set; }
    }
}

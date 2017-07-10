// Machina ~ NetworkPacket.cs
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

namespace Machina
{
    public class NetworkPacket
    {
        public byte[] Buffer { get; set; }
        public bool Push { get; set; }
        public uint TCPSequence { get; set; }
        public uint Key { get; set; }
        public int CurrentPosition { get; set; }
        public int MessageSize { get; set; }
        public DateTime PacketDate { get; set; }
    }
}

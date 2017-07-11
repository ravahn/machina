// Machina ~ ServerConnection.cs
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
using System.Net;

namespace Machina.Models
{
    public class ServerConnection
    {
        public uint DestinationAddress;
        public ushort DestinationPort;
        public uint SourceAddress;
        public ushort SourcePort;
        public DateTime TimeStamp;

        public override bool Equals(object obj)
        {
            var connection = obj as ServerConnection;
            if (connection == null || (int) SourceAddress != (int) connection.SourceAddress || (int) DestinationAddress != (int) connection.DestinationAddress || SourcePort != connection.SourcePort)
            {
                return false;
            }
            return DestinationPort == connection.DestinationPort;
        }

        public override int GetHashCode()
        {
            return (int) (SourceAddress ^ DestinationAddress ^ SourcePort ^ DestinationPort);
        }

        public override string ToString()
        {
            return new IPEndPoint(SourceAddress, (ushort) IPAddress.NetworkToHostOrder((short) SourcePort)) + " -> " + new IPEndPoint(DestinationAddress, (ushort) IPAddress.NetworkToHostOrder((short) DestinationPort));
        }
    }
}

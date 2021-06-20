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

using System.Net;
using Machina.Decoders;
using Machina.Sockets;

namespace Machina.Infrastructure
{
    public class TCPConnection
    {
        public uint LocalIP { get; set; }
        public ushort LocalPort { get; set; }
        public uint RemoteIP { get; set; }
        public ushort RemotePort { get; set; }

        public uint ProcessId { get; set; }

        public string ID => ToString();

        internal ICaptureSocket Socket { get; set; }

        internal IPDecoder IPDecoderReceive
        { get; set; }
        internal IPDecoder IPDecoderSend
        { get; set; }
        internal TCPDecoder TCPDecoderReceive
        { get; set; }
        internal TCPDecoder TCPDecoderSend
        { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TCPConnection c
                && LocalIP == c.LocalIP
                && LocalPort == c.LocalPort
                && RemoteIP == c.RemoteIP
                && RemotePort == c.RemotePort;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (int)(LocalIP ^ LocalPort ^ RemoteIP ^ RemotePort);
            }
        }

        public override string ToString()
        {
            return $"{new IPAddress(LocalIP)}:{LocalPort}=>" +
                $"{new IPAddress(RemoteIP)}:{RemotePort}({ProcessId})";
        }
    }

}

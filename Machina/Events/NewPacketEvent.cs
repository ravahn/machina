// Machina ~ NewPacketEvent.cs
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
using NLog;

namespace Machina.Events
{
    public class NewNetworkPacketEvent : EventArgs
    {
        public NewNetworkPacketEvent(object sender, Logger logger, NetworkPacket networkPacket)
        {
            Sender = sender;
            Logger = logger;
            NetworkPacket = networkPacket;
        }

        public object Sender { get; set; }
        public Logger Logger { get; set; }
        public NetworkPacket NetworkPacket { get; set; }
    }
}

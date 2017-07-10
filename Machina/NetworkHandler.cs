// Machina ~ NetworkHandler.cs
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
using Machina.Events;
using NLog;

namespace Machina
{
    public class NetworkHandler
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        public NetworkHandler()
        {
            
        }

        ~NetworkHandler()
        {
        }

        #region Event Raising

        public event EventHandler<ExceptionEvent> ExceptionEvent = delegate { };

        protected internal virtual void RaiseException(Logger logger, Exception e, bool levelIsError = false)
        {
            ExceptionEvent?.Invoke(this, new ExceptionEvent(this, logger, e, levelIsError));
        }

        public event EventHandler<NewNetworkPacketEvent> NewNetworkPacketEvent = delegate { };

        protected internal virtual void RaiseNewPacket(Logger logger, NetworkPacket networkPacket)
        {
            NewNetworkPacketEvent?.Invoke(this, new NewNetworkPacketEvent(this, logger, networkPacket));
        }

        #endregion

        #region Property Bindings

        private static Lazy<NetworkHandler> _instance = new Lazy<NetworkHandler>(() => new NetworkHandler());

        public static NetworkHandler Instance
        {
            get { return _instance.Value; }
        }

        #endregion
    }
}

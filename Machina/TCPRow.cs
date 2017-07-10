// Machina ~ TCPRow.cs
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

using System.Net;
using System.Net.NetworkInformation;

namespace Machina
{
    public class TCPRow
    {
        #region Constructors

        public TCPRow(UnsafeNativeMethods.TCPRow row)
        {
            state = row.State;
            processId = row.ProcessID;
            var localPort = (row.LocalPort1 << 8) + row.LocalPort2 + (row.LocalPort3 << 24) + (row.LocalPort4 << 16);
            long localAddress = row.LocalAddress;
            localEndPoint = new IPEndPoint(localAddress, localPort);
            var remotePort = (row.RemotePort1 << 8) + row.RemotePort2 + (row.RemotePort3 << 24) + (row.RemotePort4 << 16);
            long remoteAddress = row.RemoteAddress;
            remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }

        #endregion

        #region Private Fields

        private IPEndPoint localEndPoint;
        private int processId;
        private IPEndPoint remoteEndPoint;
        private TcpState state;

        #endregion

        #region Public Properties

        public IPEndPoint LocalEndPoint
        {
            get { return localEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return remoteEndPoint; }
        }

        public TcpState State
        {
            get { return state; }
        }

        public int ProcessId
        {
            get { return processId; }
        }

        #endregion
    }
}

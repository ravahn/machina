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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Machina.Decoders;
using Machina.Headers;
using Machina.Infrastructure;
using Machina.Sockets;

namespace Machina
{
    public class ConnectionManager : IDisposable
    {
        public TCPNetworkMonitorConfig Config { get; } = new TCPNetworkMonitorConfig();
        public IList<TCPConnection> Connections { get; } = new List<TCPConnection>(2);


        private readonly ProcessTCPInfo _processTCPInfo = new ProcessTCPInfo();
        private bool _disposedValue;

        public void Refresh()
        {
            // Update any filters
            _processTCPInfo.ProcessID = Config.ProcessID;
            _processTCPInfo.ProcessIDList = Config.ProcessIDList;
            _processTCPInfo.ProcessWindowName = Config.WindowName;
            _processTCPInfo.ProcessWindowClass = Config.WindowClass;
            _processTCPInfo.LocalIP = Config.LocalIP;

            // todo: do not pass in current connections?
            // get any active game connections
            _processTCPInfo.UpdateTCPIPConnections(Connections);

            foreach (TCPConnection connection in Connections)
            {
                if (connection.Socket == null)
                {
                    // Set up decoders for data sent from local machine
                    connection.IPDecoderSend = new IPDecoder(connection.LocalIP, connection.RemoteIP, IPProtocol.TCP);
                    connection.TCPDecoderSend = new TCPDecoder(connection.LocalPort, connection.RemotePort);

                    // set up decoders for data received by local machine
                    connection.IPDecoderReceive = new IPDecoder(connection.RemoteIP, connection.LocalIP, IPProtocol.TCP);
                    connection.TCPDecoderReceive = new TCPDecoder(connection.RemotePort, connection.LocalPort);

                    // set up socket
                    connection.Socket = Config.MonitorType == NetworkMonitorType.WinPCap ?
                        new PCapCaptureSocket(Config.RPCap) :
                        (ICaptureSocket)new RawCaptureSocket();

                    connection.Socket.StartCapture(connection.LocalIP, Config.UseRemoteIpFilter ? connection.RemoteIP : 0);
                }
            }
        }

        public void Cleanup()
        {
            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections[i].Socket != null)
                {
                    Trace.WriteLine("TCPNetworkMonitor: Stopping " + Config.MonitorType.ToString() + " listener between [" +
                        new IPAddress(Connections[i].LocalIP).ToString() + "] => [" +
                        new IPAddress(Connections[i].RemoteIP).ToString() + "].", "DEBUG-MACHINA");

                    Connections[i].Socket.StopCapture();
                    Connections[i].Socket?.Dispose();
                    Connections[i].Socket = null;
                }
            }

            Connections.Clear();
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < Connections.Count; i++)
                    {
                        // Note: Do not call Trace in Dispose()
                        Connections[i].Socket?.StopCapture();
                        Connections[i].Socket?.Dispose();
                        Connections[i].Socket = null;
                    }
                    Connections.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

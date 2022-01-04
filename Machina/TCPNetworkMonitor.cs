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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Machina.Infrastructure;
using Machina.Sockets;

namespace Machina
{
    /// <summary>
    /// TCPNetworkMonitor: This class uses either Winsock raw socket or a PCap kernel driver to read local network traffic.  A C# task polls the received data and 
    ///   raises it to event listeners synchronously.  Data is only captured for the specified processes.  
    ///   
    ///   It is configured through the following properties, which are exposed on the .Config property:
    ///     MonitorType: Specifies whether it should use a winsock raw socket, or use PCap (requires separate kernel driver installation).  Default is a raw socket.
    ///     ProcessID: Specifies a single process ID to record traffic from
    ///     ProcessIDList: Specifies a collection of process IDs to record traffic from, to support collecting data from multiple processes at the same time
    ///     WindowName: Specifies the window name to record traffic from, where process ID is unavailable
    ///     WindowClass: Specifies the window class to record traffic from, where process ID is unavailable
    ///     UseRemoteIpFilter: boolean that specifies whether to start data capture as connections are detected within the target process (new behavior), or monitor
    ///       the primary interface for the process and capture all data sent/received on that interface - and filter it.  The new behavior may cause some data to be lost 
    ///       on connection startup, but significantly reduces the filtering overhead caused by other traffic on the network interface.
    ///     RPCap: specifies configuration properties to engage RPcap uri-based network parameters
    ///     
    ///   In addition to the configuration properties, two public delegates are exposed
    ///     DataReceivedEventHandler: Delegate that is called when data is received and successfully decoded through IP and TCP decoders.  Note that a connection identifier is 
    ///       supplied to distinguish between multiple connections.
    ///     DataSentEventHandler: Delegate that is called when data is sent and successfully decoded through IP and TCP decoders.  Note that a connection identifier is 
    ///       supplied to distinguish between multiple connections.
    /// </summary>
    public class TCPNetworkMonitor : IDisposable
    {
        /// <summary>
        /// Specifies configuration values for the TCP monitor
        /// </summary>
        public TCPNetworkMonitorConfig Config => _connectionManager.Config;

        #region Data Delegates with Process Id section
        public delegate void DataReceivedDelegate2(TCPConnection connection, byte[] data);

        /// <summary>
        /// Specifies the delegate that is called when data is received and successfully decoded.
        /// </summary>
        public DataReceivedDelegate2 DataReceivedEventHandler;

        public void OnDataReceived(TCPConnection connection, byte[] data)
        {
            DataReceivedEventHandler?.Invoke(connection, data);
        }

        public delegate void DataSentDelegate2(TCPConnection connection, byte[] data);

        public DataSentDelegate2 DataSentEventHandler;

        public void OnDataSent(TCPConnection connection, byte[] data)
        {
            DataSentEventHandler?.Invoke(connection, data);
        }
        #endregion

        private readonly ConnectionManager _connectionManager = new ConnectionManager();
        private Task _monitorTask;
        private CancellationTokenSource _tokenSource;

        private bool _disposedValue;
        private DateTime _lastLoopError = DateTime.MinValue;

        /// <summary>
        /// Validates the parameters and starts the monitor.
        /// </summary>
        public void Start()
        {
            if (Config.ProcessID == 0 && string.IsNullOrWhiteSpace(Config.WindowName) && string.IsNullOrWhiteSpace(Config.WindowClass))
                throw new ArgumentException("TCPNetworkMonitor: Please specify one of Config.ProcessID, Config.ProcessIDList, Config.WindowName or Config.WindowClass.");
            if (DataReceivedEventHandler == null && DataSentEventHandler == null)
                throw new ArgumentException("TCPNetworkMonitor: Please set DataReceivedEventHandler and/or DataSentEventHandler.");

            _tokenSource = new CancellationTokenSource();
            _monitorTask = Task.Run(() => ProcessDataLoop(_tokenSource.Token));
        }

        /// <summary>
        /// Stops the monitor if it is active.
        /// </summary>
        public void Stop()
        {
            _tokenSource?.Cancel();

            if (_monitorTask != null)
            {
                if (!_monitorTask.Wait(100) || _monitorTask.Status == TaskStatus.Running)
                    Trace.Write("TCPNetworkMonitor: Task cannot be stopped.", "DEBUG-MACHINA");
                else
                    _monitorTask.Dispose();
                _monitorTask = null;
            }

            _tokenSource?.Dispose();
            _tokenSource = null;

            _connectionManager.Cleanup();
        }

        private void ProcessDataLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _connectionManager.Refresh();

                        ProcessNetworkData();

                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        if (DateTime.UtcNow.Subtract(_lastLoopError).TotalSeconds > 5)
                            Trace.WriteLine("TCPNetworkMonitor Error in ProcessDataLoop inner code: " + ex.ToString(), "DEBUG-MACHINA");
                        _lastLoopError = DateTime.UtcNow;
                    }

                    Task.Delay(30, token).Wait(token);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Trace.WriteLine("TCPNetworkMonitor Error in ProcessDataLoop: " + ex.ToString(), "DEBUG-MACHINA");
            }
        }

        private void ProcessNetworkData()
        {
            byte[] tcpbuffer;
            byte[] payloadBuffer;

            for (int i = 0; i < _connectionManager.Connections.Count; i++)
            {
                TCPConnection connection = _connectionManager.Connections[i];
                CapturedData data;

                while ((data = connection.Socket.Receive()).Size > 0)
                {
                    connection.IPDecoderSend.FilterAndStoreData(data.Buffer, data.Size);

                    while ((tcpbuffer = connection.IPDecoderSend.GetNextIPPayload()) != null)
                    {
                        connection.TCPDecoderSend.FilterAndStoreData(tcpbuffer);
                        while ((payloadBuffer = connection.TCPDecoderSend.GetNextTCPDatagram()) != null)
                            OnDataSent(connection, payloadBuffer);
                    }

                    connection.IPDecoderReceive.FilterAndStoreData(data.Buffer, data.Size);
                    while ((tcpbuffer = connection.IPDecoderReceive.GetNextIPPayload()) != null)
                    {
                        connection.TCPDecoderReceive.FilterAndStoreData(tcpbuffer);
                        while ((payloadBuffer = connection.TCPDecoderReceive.GetNextTCPDatagram()) != null)
                            OnDataReceived(connection, payloadBuffer);
                    }
                }
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _monitorTask?.Dispose();
                    _tokenSource?.Dispose();
                    _connectionManager.Dispose();
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

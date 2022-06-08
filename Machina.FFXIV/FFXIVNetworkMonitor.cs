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
using System.Net;
using Machina.Infrastructure;

namespace Machina.FFXIV
{
    /// <summary>
    /// FFXIVNetworkMonitor is configured through the following properties after it is constructed:
    ///   MonitorType: Specifies whether it should use a winsock raw socket, or use WinPCap (requires separate kernel driver installation).  Default is a raw socket.
    ///   ProcessID: (optional) Specifies the process ID to record traffic from
    ///   ProcessIDList: Specifies a collection of process IDs to record traffic from
    ///     
    /// This class uses the Machina.TCPNetworkMonitor class to find and monitor the communication from Final Fantasy XIV.  It decodes the data thaat adheres to the
    ///   FFXIV network packet format and calls the message delegate when data is received.
    /// </summary>
    public class FFXIVNetworkMonitor : IDisposable
    {

        /// <summary>
        /// Specifies the type of monitor to use - Raw socket or WinPCap
        /// </summary>
        public NetworkMonitorType MonitorType
        { get; set; } = NetworkMonitorType.RawSocket;

        /// <summary>
        /// Specifies the Process ID that is generating or receiving the traffic.  Either ProcessID, ProcessIDList, or WindowName must be specified.
        /// </summary>
        public uint ProcessID
        { get; set; }

        public ICollection<uint> ProcessIDList
        { get; } = new List<uint>();

        /// <summary>
        /// Specifies the local IP address to override the detected IP
        /// </summary>
        public IPAddress LocalIP
        { get; set; } = IPAddress.None;

        /// <summary>
        /// Specifies whether to use Winsock/WinPcap server IP filtering instead of filtering in code
        ///   This has a small chance of losing data when new TCP sockets connect, but significantly reduces data processing overhead.
        /// </summary>
        public bool UseRemoteIpFilter
        { get; set; }

        /// <summary>
        /// This class keeps the information needed to authenticate the user on a remote machine or read a local file via PCap.
        /// The remote machine can either grant or refuse the access according to the information provided. In case the NULL authentication is required, both 'username' and 'password' can be NULL pointers.
        /// </summary>
        public TCPNetworkMonitorConfig.RPCapConf RPCap
        { get; set; } = new TCPNetworkMonitorConfig.RPCapConf();

        public string FFXIVDX11ExecutablePath
        { get; set; } = @"C:\Program Files (x86)\FINAL FANTASY XIV - A Realm Reborn\game\ffxiv_dx11.exe";

        #region Message Delegates section
        public delegate void MessageReceived2(TCPConnection connection, long epoch, byte[] message);

        /// <summary>
        /// Specifies the delegate that is called when data is received and successfully decoded.
        /// </summary>
        public MessageReceived2 MessageReceivedEventHandler;

        public void OnMessageReceived(TCPConnection connection, long epoch, byte[] message)
        {
            MessageReceivedEventHandler?.Invoke(connection, epoch, message);
        }

        public delegate void MessageSent2(TCPConnection connection, long epoch, byte[] message);

        public MessageSent2 MessageSentEventHandler;

        public void OnMessageSent(TCPConnection connection, long epoch, byte[] message)
        {
            MessageSentEventHandler?.Invoke(connection, epoch, message);
        }

        #endregion

        private TCPNetworkMonitor _monitor;
        private bool _disposedValue;

        private readonly Dictionary<string, FFXIVBundleDecoder> _sentDecoders = new Dictionary<string, FFXIVBundleDecoder>();
        private readonly Dictionary<string, FFXIVBundleDecoder> _receivedDecoders = new Dictionary<string, FFXIVBundleDecoder>();

        /// <summary>
        /// Validates the parameters and starts the monitor.
        /// </summary>
        public void Start()
        {
            if (_monitor != null)
            {
                _monitor.Stop();
                _monitor = null;
            }

            if (MessageReceivedEventHandler == null)
                throw new ArgumentException("MessageReceived delegate must be specified.");

            _monitor = new TCPNetworkMonitor();
            _monitor.Config.ProcessID = ProcessID;
            _monitor.Config.ProcessIDList = ProcessIDList;
            if (_monitor.Config.ProcessID == 0)
                _monitor.Config.WindowName = "FINAL FANTASY XIV";
            _monitor.Config.MonitorType = MonitorType;
            _monitor.Config.LocalIP = LocalIP;
            _monitor.Config.UseRemoteIpFilter = UseRemoteIpFilter;
            _monitor.Config.RPCap = RPCap;

            _monitor.DataSentEventHandler = (TCPConnection connection, byte[] data) => ProcessSentMessage(connection, data);
            _monitor.DataReceivedEventHandler = (TCPConnection connection, byte[] data) => ProcessReceivedMessage(connection, data);

            // initialize Oodle static
            FFXIVOodle_Native.Initialize(FFXIVDX11ExecutablePath);

            _monitor.Start();
        }

        /// <summary>
        /// Stops the monitor if it is active.
        /// </summary>
        public void Stop()
        {
            _monitor.DataSentEventHandler = null;
            _monitor.DataReceivedEventHandler = null;
            _monitor.Stop();
            _monitor.Dispose();
            _monitor = null;

            _sentDecoders.Clear();
            _receivedDecoders.Clear();
        }

        public void ProcessSentMessage(TCPConnection connection, byte[] data)
        {
            Tuple<long, byte[]> message;
            if (!_sentDecoders.ContainsKey(connection.ID))
                _sentDecoders.Add(connection.ID, new FFXIVBundleDecoder());

            _sentDecoders[connection.ID].StoreData(data);
            while ((message = _sentDecoders[connection.ID].GetNextFFXIVMessage()) != null)
            {
                OnMessageSent(connection, message.Item1, message.Item2);
            }
        }

        public void ProcessReceivedMessage(TCPConnection connection, byte[] data)
        {
            Tuple<long, byte[]> message;
            if (!_receivedDecoders.ContainsKey(connection.ID))
                _receivedDecoders.Add(connection.ID, new FFXIVBundleDecoder());

            _receivedDecoders[connection.ID].StoreData(data);
            while ((message = _receivedDecoders[connection.ID].GetNextFFXIVMessage()) != null)
            {
                OnMessageReceived(connection, message.Item1, message.Item2);
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _monitor?.Dispose();
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
    }
}

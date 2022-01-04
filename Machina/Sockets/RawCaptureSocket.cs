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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Machina.Infrastructure;

namespace Machina.Sockets
{
    public class RawCaptureSocket : ICaptureSocket
    {
        private readonly int BUFFER_SIZE = (1024 * 64) + 1;

        private readonly object _lockObject = new object();
        private Socket _socket;

        private readonly ConcurrentQueue<Tuple<byte[], int>> _pendingBuffers = new ConcurrentQueue<Tuple<byte[], int>>();
        private byte[] _currentBuffer;

        private bool _disposedValue;

        private static readonly bool _isWindows = IsWindows();

        public void StartCapture(uint localAddress, uint remoteAddress = 0)
        {
            lock (_lockObject)
            {
                // create the socket
                _socket = CreateRawSocket(localAddress, remoteAddress);

                // set buffer
                _currentBuffer = new byte[BUFFER_SIZE];

                // start receiving data asynchronously
                _ = _socket.BeginReceive(_currentBuffer, 0, _currentBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
        }

        public void StopCapture()
        {
            lock (_lockObject)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                        _socket.Dispose();
                    }
                    finally
                    {
                        _socket = null;
                    }
                }
            }

            FreeBuffers();
        }

        public CapturedData Receive()
        {
            if (_pendingBuffers.TryDequeue(out Tuple<byte[], int> next))
                return new CapturedData { Buffer = next.Item1, Size = next.Item2 };

            return new CapturedData { Buffer = null, Size = 0 };
        }

        private Socket CreateRawSocket(uint localAddress, uint remoteAddress)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, _isWindows ? ProtocolType.IP : ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(localAddress, 0));

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

            if (_isWindows)
            {
                byte[] trueBytes = new byte[4] { 3, 0, 0, 0 }; // 3 == RCVALL_IPLEVEL, so it only intercepts the target interface
                byte[] outBytes = new byte[4];
                _ = socket.IOControl(IOControlCode.ReceiveAll, trueBytes, outBytes);
            }

            if (remoteAddress > 0)
                socket.Connect(new IPEndPoint(remoteAddress, 0));

            return socket;
        }

        private static bool IsWindows()
        {
            const string WINE_ENV_VAR = "FORCE_MACHINA_RAW_SOCKET_WINE_COMPAT";

            Process[] processes = Process.GetProcessesByName("Idle");
            if (processes.Length == 0)
            {
                Trace.WriteLine("RawCaptureSocket: Did not detect Idle process, using TCP socket instead of IP socket for wine compatability.", "DEBUG-MACHINA");
                return false;
            }

            if (Environment.GetEnvironmentVariable(WINE_ENV_VAR) == "1")
            {
                Trace.WriteLine($"RawCaptureSocket: Environment variable {WINE_ENV_VAR} set, forcing wine compatability.", "DEBUG-MACHINA");
                return false;
            }

            return true;
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_socket == null)
                        return;

                    byte[] buffer = _currentBuffer;

                    int received = _socket.EndReceive(ar);
                    if (received > 0)
                        _currentBuffer = new byte[BUFFER_SIZE];

                    _ = _socket.BeginReceive(_currentBuffer, 0, _currentBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);

                    if (received > 0)
                        _pendingBuffers.Enqueue(new Tuple<byte[], int>(buffer, received));
                }
            }
            catch (ObjectDisposedException)
            {
                // do nothing - teardown occurring.
            }
            catch (Exception ex)
            {
                Trace.WriteLine("RawSocket: Error while receiving socket data.  Network capture aborted, please restart application." + ex.ToString(), "DEBUG-MACHINA");
            }
        }

        private void FreeBuffers()
        {
            if (_currentBuffer != null)
            {
                _currentBuffer = null;
            }

            while (_pendingBuffers.TryDequeue(out Tuple<byte[], int> _))
            {

            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _socket?.Dispose();
                    _socket = null;

                    FreeBuffers();
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

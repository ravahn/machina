// Machina ~ RawSocket.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Machina
{
    public class RawSocket
    {
        private Socket _socket = null;
        private object _socketLock = new object();
        private NetworkBufferFactory _bufferFactory = new NetworkBufferFactory(0, 0);

        public RawSocket() : this(true) { }

        public RawSocket(bool UseSustainedLowLatencyGC)
        {
            // force sustained low latency garbage collection
            if (UseSustainedLowLatencyGC)
                if (System.Runtime.GCSettings.LatencyMode != System.Runtime.GCLatencyMode.SustainedLowLatency)
                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
        }

        public void Create(uint localAddress, uint remoteAddress)
        {
            // create the socket
            lock (_socketLock)
            {
                _socket = CreateRawSocket(localAddress, remoteAddress);

                // start receiving data
                Buffer buffer = _bufferFactory.GetNextFreeBuffer();
                _socket.BeginReceive(buffer.Data, 0, buffer.Data.Length, SocketFlags.None, new System.AsyncCallback(OnReceive), (object)buffer);
            }
        }

        public int Receive(out byte[] buffer)
        {
            // retrieve data from allocated buffer.
            Buffer data = _bufferFactory.GetNextAllocatedBuffer();
            buffer = data?.Data;
            return data?.AllocatedSize ?? 0;
        }

        public void Destroy()
        {
            lock (_socketLock)
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
        }

        private static Socket CreateRawSocket(uint localAddress, uint remoteAddress)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            socket.Bind(new IPEndPoint(localAddress, 0));

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

            byte[] trueBytes = new byte[4] { 3, 0, 0, 0 }; // 3 == RCVALL_IPLEVEL, so it only intercepts the target interface
            byte[] outBytes = new byte[4];

            socket.IOControl(IOControlCode.ReceiveAll, trueBytes, outBytes);

            socket.ReceiveBufferSize = 1024 * 50000; // this is the size of the internal network card buffer.   It must be large due to thread pre-emption.

            if (remoteAddress != 0)
                socket.Connect(new IPEndPoint(remoteAddress, 0));

            return socket;
        }

        private void OnReceive(IAsyncResult ar)
        {
            // Note: this is instanced, so need to take care to
            try
            {
                Buffer state = (Buffer)ar.AsyncState;

                state = _bufferFactory.GetNextFreeBuffer();

                //lock (_socketLock)
                //{
                    if (this._socket == null)
                        return;

                    int received = this._socket.EndReceive(ar);
                    if (received > 0)
                    {
                        state.AllocatedSize = received;
                        _bufferFactory.AddAllocatedBuffer(state);
                    }


                    _socket.BeginReceive(state.Data, 0, state.Data.Length, SocketFlags.None, new System.AsyncCallback(OnReceive), (object)state);
                //}
            }
            catch (ObjectDisposedException)
            {
                // do nothing - teardown occurring.
            }
            catch (Exception ex)
            {
                Trace.Write("Error while receiving socket data.  Network capture aborted, please restart application." + ex.ToString());
            }
        }
    }

}

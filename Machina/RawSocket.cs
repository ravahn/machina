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

using System.Net;
using System.Net.Sockets;

namespace Machina
{
    public class RawSocket : IRawSocket
    {
        private Socket _receiveSocket = null;
        private byte[] _receiveBuffer = null;

        public uint LocalIP
        { get; private set; }
        public uint RemoteIP
        { get; private set; }

        public void Create(uint localAddress, uint remoteAddress = 0)
        {
            LocalIP = localAddress;
            RemoteIP = remoteAddress;

            _receiveSocket = CreateRawSocket(localAddress, remoteAddress);
            _receiveBuffer = new byte[1024 * 128];
        }

        public int Receive(out byte[] buffer)
        {
            buffer = _receiveBuffer;

            if (_receiveSocket == null)
                return 0;
            if (_receiveSocket.Available == 0)
                return 0;

            return _receiveSocket.Receive(_receiveBuffer);
        }

        public void Destroy()
        {
            if (_receiveSocket != null)
            {
                try
                {
                    _receiveSocket.Shutdown(SocketShutdown.Both);
                    _receiveSocket.Close();
                    _receiveSocket.Dispose();
                }
                finally
                {
                    _receiveSocket = null;
                }
            }
        }

        private Socket CreateRawSocket(uint localAddress, uint remoteAddress)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            socket.Bind(new IPEndPoint(localAddress, 0));

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

            byte[] trueBytes = new byte[4] { 3, 0, 0, 0 }; // 3 == RCVALL_IPLEVEL, so it only intercepts the target interface
            byte[] outBytes = new byte[4];

            socket.IOControl(IOControlCode.ReceiveAll, trueBytes, outBytes);

            socket.ReceiveBufferSize = 1024 * 5000; // this is the size of the internal network card buffer

            if (remoteAddress > 0)
                socket.Connect(new IPEndPoint(remoteAddress, 0));

            return socket;
        }

    }

}

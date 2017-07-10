// Machina ~ UnsafeNativeMethods.cs
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
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Machina
{
    public static class UnsafeNativeMethods
    {
        public enum TCP_TABLE_CLASS
        {
            BASIC_LISTENER,
            BASIC_CONNECTIONS,
            BASIC_ALL,
            OWNER_PID_LISTENER,
            OWNER_PID_CONNECTIONS,
            OWNER_PID_ALL,
            OWNER_MODULE_LISTENER,
            OWNER_MODULE_CONNECTIONS,
            OWNER_MODULE_ALL
        }

        [DllImport("iphlpapi.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, uint ipVersion, TCP_TABLE_CLASS tcpTableClass, uint reserved = 0);

        public struct MIB_TCPROW_EX
        {
            public uint dwLocalAddr;
            public int dwLocalPort;
            public int dwProcessId;
            public uint dwRemoteAddr;
            public int dwRemotePort;
            public TcpState dwState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCPRow
        {
            public TcpState State;
            public uint LocalAddress;
            public byte LocalPort1;
            public byte LocalPort2;
            public byte LocalPort3;
            public byte LocalPort4;
            public uint RemoteAddress;
            public byte RemotePort1;
            public byte RemotePort2;
            public byte RemotePort3;
            public byte RemotePort4;
            public int ProcessID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TCPTable
        {
            public uint Length;
            private TCPRow Row;
        }
    }
}

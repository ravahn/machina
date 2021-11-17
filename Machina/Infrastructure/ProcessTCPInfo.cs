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
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Machina.Infrastructure
{
    /// <summary>
    /// Manages access to the TCP table and assists with tracking when the connections change per-process
    /// </summary>
    public unsafe class ProcessTCPInfo
    {

        #region WIN32 TCP Table
        // DLLImport
        [DllImport("iphlpapi.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool bOrder, uint /*ulong*/ dwFamily, TCP_TABLE_CLASS dwClass, uint /*ulong*/ dwReserved);

        // TCP Table Enum
#pragma warning disable CA1707 // Identifiers should not contain underscores
        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        // disable warning about this not being assigned in code, which is untrue since it is done via Marshal class.
#pragma warning disable 0649
        // TCP Row Structure
        private struct MIB_TCPROW_EX
        {
            public TcpState dwState;
            public uint dwLocalAddr;
            public int dwLocalPort;
            public uint dwRemoteAddr;
            public int dwRemotePort;
            public uint dwProcessId;
        }
        private const int AF_INET = 2;

#pragma warning restore 0649
        #endregion

        /// <summary>
        /// Process ID of the process to return network connection information about
        /// </summary>
        public uint ProcessID
        { get; set; }

        /// <summary>
        /// Process ID of the process to return network connection information about
        /// </summary>
        public ICollection<uint> ProcessIDList
        { get; set; } = new List<uint>();

        /// <summary>
        /// Window text of the process to return network connection information about
        /// </summary>
        public string ProcessWindowName
        { get; set; } = "";

        /// <summary>
        /// Window class of the process to return network connection information about
        /// </summary>
        public string ProcessWindowClass
        { get; set; } = "";

        /// <summary>
        /// Sets the local IP address of the network interface to monitor.
        /// </summary>
        public IPAddress LocalIP
        { get; set; } = IPAddress.None;


        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string sClass, string sWindow);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Unicode)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// This returns the process id of all windows with the specified window name or class.
        /// </summary>
        /// <param name="windowClass">class of the window to look for</param> 
        /// <param name="windowName">name of the window to look for</param> 
        /// <returns>Process ID List</returns>
        public static IList<uint> GetProcessIDByWindow(string windowClass, string windowName)
        {
            List<uint> processIDList = new List<uint>();
            IntPtr hWindow = IntPtr.Zero;

            while ((hWindow = FindWindowEx(IntPtr.Zero, hWindow, windowClass, windowName)) != IntPtr.Zero)
            {
                _ = GetWindowThreadProcessId(hWindow, out uint processID);
                if (processID > 0)
                    processIDList.Add(processID);
            }
            return processIDList;
        }

        /// <summary>
        /// This retrieves all current TCPIP connections, filters them based on a process id (specified by either ProcessID, ProcessWindowName or ProcessWindowClass parameter),
        ///   and updates the connections collection.
        /// </summary>
        /// <param name="connections">List containing prior connections that needs to be maintained</param>
        public unsafe void UpdateTCPIPConnections(IList<TCPConnection> connections)
        {
            List<uint> _processFilterList = new List<uint>();
            _processFilterList.Clear();

            if (ProcessIDList.Count > 0)
                _processFilterList.AddRange(ProcessIDList);
            else if (ProcessID > 0)
                _processFilterList.Add(ProcessID);
            else if (!string.IsNullOrWhiteSpace(ProcessWindowClass)) // i prefer it first since it's language irrelevant, ascii only and constant until the window being destroyed.
                _processFilterList.AddRange(GetProcessIDByWindow(ProcessWindowClass, null));
            else if (!string.IsNullOrWhiteSpace(ProcessWindowName))
                _processFilterList.AddRange(GetProcessIDByWindow(null, ProcessWindowName));

            if (_processFilterList.Count == 0)
            {
                if (connections.Count > 0)
                {
                    Trace.WriteLine("ProcessTCPInfo: Process has exited, closing all connections.", "DEBUG-MACHINA");

                    connections.Clear();
                }

                return;
            }

            IntPtr ptrTCPTable = IntPtr.Zero;
            int bufferLength = 0;
            int ret = 0;
            int tcpTableCount = 0;
            IntPtr tmpPtr = IntPtr.Zero;

            // attempt to allocate 5 times, in case there are frequent increases in the # of tcp connections
            for (int i = 0; i < 5; i++)
            {
                ret = GetExtendedTcpTable(ptrTCPTable, ref bufferLength, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);

                if (ret == 0)
                    break;
                if (ptrTCPTable != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrTCPTable);
                    ptrTCPTable = IntPtr.Zero;
                }

                // fix for constant connection churn
                bufferLength *= 2;

                ptrTCPTable = Marshal.AllocHGlobal(bufferLength);
            }

            try
            {
                if (ret == 0)
                {
                    //retrieving numbers of entries
                    tcpTableCount = *(int*)ptrTCPTable;
                    tmpPtr = ptrTCPTable + sizeof(int);

                    for (int i = 0; i <= tcpTableCount - 1; i++)
                    {
                        MIB_TCPROW_EX entry = *(MIB_TCPROW_EX*)tmpPtr;

                        // Process if ProcessID matches
                        if (_processFilterList.Contains(entry.dwProcessId))
                        {
                            bool bFound = false;
                            for (int j = 0; j < connections.Count; j++)
                            {
                                if (connections[j].LocalIP == entry.dwLocalAddr &&
                                    connections[j].RemoteIP == entry.dwRemoteAddr &&
                                    connections[j].LocalPort == (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwLocalPort) &&
                                    connections[j].RemotePort == (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwRemotePort)
                                    )
                                {
                                    bFound = true;
                                    break;
                                }
                            }

                            if (!bFound && entry.dwLocalAddr != 0)
                            {
                                TCPConnection connection = new TCPConnection()
                                {
                                    LocalIP = entry.dwLocalAddr,
                                    RemoteIP = entry.dwRemoteAddr,
                                    LocalPort = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwLocalPort),
                                    RemotePort = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwRemotePort),
                                    ProcessId = entry.dwProcessId
                                };

                                connections.Add(connection);

                                Trace.WriteLine($"ProcessTCPInfo: New connection detected for Process [{entry.dwProcessId}]: {connection}", "DEBUG-MACHINA");
                            }
                        }

                        // increment pointer
                        tmpPtr += sizeof(MIB_TCPROW_EX);
                    }

                    for (int i = connections.Count - 1; i >= 0; i--)
                    {
                        bool bFound = false;
                        tmpPtr = ptrTCPTable + sizeof(int);

                        for (int j = 0; j <= tcpTableCount - 1; j++)
                        {
                            MIB_TCPROW_EX entry = *(MIB_TCPROW_EX*)tmpPtr;

                            // Process if ProcessID matches
                            if (_processFilterList.Contains(entry.dwProcessId))
                            {
                                if (connections[i].LocalIP == entry.dwLocalAddr &&
                                    connections[i].RemoteIP == entry.dwRemoteAddr &&
                                    connections[i].LocalPort == (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwLocalPort) &&
                                    connections[i].RemotePort == (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwRemotePort)
                                    )
                                {
                                    bFound = true;
                                    break;
                                }
                            }
                            // increment pointer
                            tmpPtr += sizeof(MIB_TCPROW_EX);
                        }
                        if (!bFound)
                        {
                            Trace.WriteLine($"ProcessTCPInfo: Removed connection {connections[i]}", "DEBUG-MACHINA");
                            connections[i].Socket.StopCapture();
                            connections.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    Trace.WriteLine($"ProcessTCPInfo: Unable to retrieve TCP table. Return code: {ret}", "DEBUG-MACHINA");
                    throw new System.ComponentModel.Win32Exception(ret);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ProcessTCPInfo: Exception updating TCP connection list." + ex.ToString(), "DEBUG-MACHINA");
                throw new System.ComponentModel.Win32Exception(ret, ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrTCPTable);
            }
        }
    }
}

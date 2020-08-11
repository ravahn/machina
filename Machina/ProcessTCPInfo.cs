// Machina ~ ProcessTCPInfo.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Machina
{
    /// <summary>
    /// Manages access to the TCP table and assists with tracking when the connections change per-process
    /// </summary>
    public unsafe class ProcessTCPInfo
    {

        #region WIN32 TCP Table
        // DLLImport
        [DllImport("iphlpapi.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool bOrder, UInt32 /*ulong*/ dwFamily, TCP_TABLE_CLASS dwClass, UInt32 /*ulong*/ dwReserved);

        // TCP Table Enum
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
            public UInt32 dwLocalAddr;
            public int dwLocalPort;
            public UInt32 dwRemoteAddr;
            public int dwRemotePort;
            public int dwProcessId;
        }
        private const Int32 AF_INET = 2;

#pragma warning restore 0649
        #endregion

        /// <summary>
        /// Process ID of the process to return network connection information about
        /// </summary>
        public uint ProcessID
        { get; set; } = 0;

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


        private uint _currentProcessID = 0;

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string sClass, string sWindow);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className,  string windowTitle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// This returns the process id of the first window with the specified window name.
        /// </summary>
        /// <param name="windowName">name of the window to look for</param> 
        /// <returns>Process ID</returns>
        public uint GetProcessIDByWindowName(string windowName)
        {
            uint processID;
            IntPtr hWindow = FindWindow(null, windowName);
            GetWindowThreadProcessId(hWindow, out processID);

            return processID;
        }
        
        /// <summary>
        /// This returns the process id of the first window with the specified window class.
        /// </summary>
        /// <param name="windowClass">class of the window to look for</param> 
        /// <returns>Process ID</returns>
        public uint GetProcessIDByWindowClass(string windowClass)
        {
            uint processID;
            IntPtr hWindow = FindWindowEx(IntPtr.Zero, IntPtr.Zero, windowClass, null);
            GetWindowThreadProcessId(hWindow, out processID);

            return processID;
        }

        /// <summary>
        /// This retrieves all current TCPIP connections, filters them based on a process id (specified by either ProcessID, ProcessWindowName or ProcessWindowClass parameter),
        ///   and updates the connections collection.
        /// </summary>
        /// <param name="connections">List containing prior connections that needs to be maintained</param>
        public unsafe void UpdateTCPIPConnections(List<TCPConnection> connections)
        {
            if (ProcessID > 0)
                _currentProcessID = ProcessID;
            else if(!string.IsNullOrWhiteSpace(ProcessWindowClass)) // i prefer it first since it's language irrelevant, ascii only and constant until the window being destroyed.
                _currentProcessID = GetProcessIDByWindowClass(ProcessWindowClass);
            else
                _currentProcessID = GetProcessIDByWindowName(ProcessWindowName);         

            if (_currentProcessID == 0)
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
                    tmpPtr = ptrTCPTable + sizeof(Int32);

                    for (int i = 0; i <= tcpTableCount - 1; i++)
                    {
                        MIB_TCPROW_EX entry = *(MIB_TCPROW_EX*)tmpPtr;

                        // Process if ProcessID matches
                        if (entry.dwProcessId == _currentProcessID)
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

                            if (!bFound)
                            {
                                var connection = new TCPConnection()
                                {
                                    LocalIP = entry.dwLocalAddr,
                                    RemoteIP = entry.dwRemoteAddr,
                                    LocalPort = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwLocalPort),
                                    RemotePort = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)entry.dwRemotePort)
                                };

                                connections.Add(connection);

                                Trace.WriteLine("ProcessTCPInfo: New connection detected for Process [" + _currentProcessID.ToString() + "]: " + connection.ToString(), "DEBUG-MACHINA");
                            }
                        }

                        // increment pointer
                        tmpPtr += sizeof(MIB_TCPROW_EX);
                    }

                    for (int i = connections.Count - 1; i >= 0; i--)
                    {
                        bool bFound = false;
                        tmpPtr = ptrTCPTable + sizeof(Int32);

                        for (int j = 0; j <= tcpTableCount - 1; j++)
                        {
                            MIB_TCPROW_EX entry = *(MIB_TCPROW_EX*)tmpPtr;

                            // Process if ProcessID matches
                            if (entry.dwProcessId == _currentProcessID)
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
                            Trace.WriteLine("ProcessTCPInfo: Removed connection " + connections[i].ToString(), "DEBUG-MACHINA");
                            connections.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    Trace.WriteLine("ProcessTCPInfo: Unable to retrieve TCP table. Return code: " + ret.ToString(), "DEBUG-MACHINA");
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

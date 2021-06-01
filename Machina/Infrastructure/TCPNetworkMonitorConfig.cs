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

using System.Collections.Generic;
using System.Net;

namespace Machina.Infrastructure
{
    public class TCPNetworkMonitorConfig
    {
        /// <summary>
        /// Specifies the type of monitor to use - Raw socket or WinPCap
        /// </summary>
        public NetworkMonitorType MonitorType
        { get; set; } = NetworkMonitorType.RawSocket;

        /// <summary>
        /// Specifies the Process ID that is generating or receiving the traffic.  Either ProcessID, ProcessIDList, WindowName or WindowClass must be specified.
        /// </summary>
        public uint ProcessID
        { get; set; }

        /// <summary>
        /// Specifies a list of process IDs to filter traffic against.
        /// </summary>
        public ICollection<uint> ProcessIDList
        { get; set; } = new List<uint>();

        /// <summary>
        /// Specifies the local IP address of the network interface to monitor
        /// </summary>
        public IPAddress LocalIP
        { get; set; } = IPAddress.None;

        /// <summary>
        /// Specifies the Window Name of the application that is generating or receiving the traffic.  Either ProcessID, ProcessIDList, WindowName or WindowClass must be specified.
        /// </summary>
        public string WindowName
        { get; set; } = "";

        /// <summary>
        /// Specifies the Window Class of the application that is generating or receiving the traffic.  Either ProcessID, ProcessIDList, WindowName or WindowClass must be specified.
        /// </summary>
        public string WindowClass
        { get; set; } = "";

        /// <summary>
        /// Specifies that the underlying sockets should filter based on both source / target IP Addresses.
        /// </summary>
        public bool UseRemoteIpFilter
        { get; set; } = true;
    }
}

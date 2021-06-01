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

namespace Machina.Sockets
{
    /// <summary>
    /// Defines the common public routines for raw socket capturea
    /// </summary>
    internal interface ICaptureSocket : IDisposable
    {
        /// <summary>
        /// Initializes the raw socket and starts the capture process
        ///   Note that remoteAddress can be used to significantly improve the reliability of capture of active connections by
        ///   offloading filtering to the winsock/winpcap layer, however if the goal is to capture all packets for a single
        ///   TCP connection, the remote address must be known and the socket created before the connection is initiated.
        ///   This is frequently impossible for monitoring third-party applications.
        /// </summary>
        /// <param name="localAddress">local IP address of the interface initiating the packets of interest</param>
        /// <param name="remoteAddress">remote IP address of the host, or 0 to capture all packets on the local interface.</param>
        void StartCapture(uint localAddress, uint remoteAddress = 0);

        /// <summary>
        /// Stops raw socket capture and cleans up any resources.
        /// </summary>
        void StopCapture();

        /// <summary>
        /// Returns both a reference to a byte array buffer, and the amount of bytes in that array containing payload data.
        ///   Size value of 0 indicates that there is no data available.
        /// </summary>
        CapturedData Receive();
    }
}

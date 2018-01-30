using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machina
{
    /// <summary>
    /// Defines the common public routines for raw socket capturea
    /// </summary>
    interface IRawSocket
    {
        /// <summary>
        /// Returns the local IP address for which the raw socket is configured.
        /// </summary>
        uint LocalIP
        { get; }

        /// <summary>
        /// Returns the remote IP address for which the raw socket is configured, or 0 if none is configured.
        /// </summary>
        uint RemoteIP
        { get; }

        /// <summary>
        /// Initializes the raw socket and starts the capture process
        ///   Note that remoteAddress can be used to significantly improve the reliability of capture of active connections by
        ///   offloading filtering to the winsock/winpcap layer, however if the goal is to capture all packets for a single
        ///   TCP connection, the remote address must be known and the socket created before the connection is initiated.
        ///   This is frequently impossible for monitoring third-party applications.
        /// </summary>
        /// <param name="localAddress">local IP address of the interface initiating the packets of interest</param>
        /// <param name="remoteAddress">remote IP address of the host, or 0 to capture all packets on the local interface.</param>
        void Create(uint localAddress, uint remoteAddress = 0);

        /// <summary>
        /// Stops raw socket capture and cleans up any resources.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Returns both a reference to a byte array buffer, and the amount of bytes in that array containing payload data.
        ///   return value of 0 indicates that there is no data available.
        /// </summary>
        int Receive(out byte[] buffer);

        /// <summary>
        /// Stores the buffer after it is processed for future reads.  This allows for fewer .Net garbage collection calls,
        ///     but is not strictly necessary for functioning.
        /// </summary>
        void FreeBuffer(ref byte[] buffer);
    }
}

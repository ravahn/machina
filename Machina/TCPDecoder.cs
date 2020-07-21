// Machina ~ TCPDecoder.cs
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

namespace Machina
{
    /// <summary>
    /// Manages reordering the TCP packets and reassembling the intact TCP stream
    ///   Note that while this will reorder the stream and reject duplicate data, it cannot control the TCP
    ///   stream and force a packet retransmission.  So, it is different from regular TCP streams.
    ///   The application must have a built-in recovery process in case data is lost or corrupted.
    /// </summary>
    public class TCPDecoder
    {
        /// <summary>
        /// Collection containing current unprocessed TCP datagrams
        /// </summary>
        public List<byte[]> Packets
        { get; }
            = new List<byte[]>();

        /// <summary>
        /// Timestamp of last processed packet
        /// </summary>
        public DateTime LastPacketTimestamp
        { get; internal set; }
            = DateTime.MinValue;

        // expected sequence number of the next TCP datagram
        private UInt32 _NextSequence = 0;

        // source port is in network byte order
        private ushort _sourcePort;
        // destination port is in network byte order
        private ushort _destinationPort;

        /// <summary>
        /// class constructor
        /// </summary>
        /// <param name="sourcePort">Source port of desired network traffic, in network byte order</param>
        /// <param name="destinationPort">Destination port of desired network traffic, in network byte order</param>
        public TCPDecoder(ushort sourcePort, ushort destinationPort)
        {
            _sourcePort = Utility.htons(sourcePort);
            _destinationPort = Utility.htons(destinationPort);
        }

        /// <summary>
        /// Takes one TCP packet as a byte array, filters it based on source/destination ports, and stores it for stream reassembly.
        /// </summary>
        /// <param name="buffer"></param>
        public unsafe void FilterAndStoreData(byte[] buffer)
        {
            // There is one TCP packet per buffer.
            if (buffer?.Length < sizeof(TCPHeader))
            {
                Trace.WriteLine("TCPDecoder: Buffer length smaller than TCP header: Length=[" + (buffer?.Length ?? 0).ToString() + "].", "DEBUG-MACHINA");
                return;
            }

            fixed (byte* ptr = buffer)
            {
                TCPHeader header = *(TCPHeader*)(ptr);

                if (_sourcePort != header.source_port ||
                    _destinationPort != header.destination_port)
                    return;

                // if there is no data, we can discard the packet - this will likely be an ACK, but it shouldnt affect stream processing.
                if (buffer.Length - header.DataOffset == 0)
                    if ((header.flags & (byte)TCPFlags.SYN) == 0)
                        return;

                //TEMP debugging
                //Trace.WriteLine("TCPDecoder: TCP Sequence # " + header.SequenceNumber.ToString() + " received with " + (buffer.Length - header.DataOffset).ToString() + " bytes.");

                Packets.Add(buffer);
            }
        }

        /// <summary>
        /// Reassembles the TCP stream from available packets.
        /// </summary>
        /// <returns>byte array containing a portion of the TCP stream payload</returns>
        public unsafe byte[] GetNextTCPDatagram()
        {
            if (Packets.Count == 0)
                return null;

            byte[] buffer = null;

            List<byte[]> packets;
            if (Packets.Count == 1)
                packets = Packets;
            else
            {
                Packets.Sort((x, y) => Utility.ntohl(BitConverter.ToUInt32(x, 4))
                    .CompareTo(Utility.ntohl(BitConverter.ToUInt32(y, 4))));
                packets = Packets;
            }
            foreach (byte[] packet in packets)
            {
                fixed (byte* ptr = packet)
                {
                    TCPHeader header = *(TCPHeader*)ptr;

                    // failsafe - if starting, or just reset, start with next available packet.
                    if (_NextSequence == 0)
                        _NextSequence = header.SequenceNumber;

                    if (header.SequenceNumber <= _NextSequence)
                    {
                        LastPacketTimestamp = DateTime.UtcNow;

                        if ((header.flags & (byte)TCPFlags.SYN) > 0)
                        {
                            // filter out only when difference between sequence numbers is ~10k
                            if (_NextSequence == 0 || _NextSequence == header.SequenceNumber)
                                _NextSequence = header.SequenceNumber + 1;
                            else if (Math.Abs(_NextSequence - header.SequenceNumber) > 100000)
                            {
                                Trace.WriteLine("TCPDecoder: Updating sequence number from SYN packet.  Current Sequence: [" + _NextSequence.ToString() + ", sent sequence: [" + header.SequenceNumber.ToString() + "].", "DEBUG-MACHINA");
                                _NextSequence = header.SequenceNumber + 1;
                            }
                            else
                                Trace.WriteLine("TCPDecoder: Ignoring SYN packet new sequence number.  Current Sequence: [" + _NextSequence.ToString() + ", sent sequence: [" + header.SequenceNumber.ToString() + "].", "DEBUG-MACHINA");

                            continue; // do not process SYN packet, but set next sequence #.
                        }

                        // if this is a retransmit, only include the portion of data that is not already processed
                        uint packetOffset = 0;
                        if (header.SequenceNumber < _NextSequence)
                            packetOffset = _NextSequence - header.SequenceNumber;

                        // do not process this packet if it was previously fully processed, or has no data.
                        if (packetOffset >= packet.Length - header.DataOffset)
                        {
                            // this packet will get removed once we exit the loop.
                            Trace.WriteLine("TCPDecoder: packet data already processed, expected sequence [" + _NextSequence.ToString() + "], received [" + header.SequenceNumber + "], size [" + (packet.Length - header.DataOffset) + "].  Data: " + Utility.ByteArrayToHexString(packet, 0, 50), "DEBUG-MACHINA");
                            continue;
                        }

                        if (buffer == null)
                        {
                            buffer = new byte[packet.Length - header.DataOffset - packetOffset];
                            Array.Copy(packet, header.DataOffset + packetOffset, buffer, 0, packet.Length - header.DataOffset - packetOffset);
                        }
                        else
                        {
                            int oldSize = buffer.Length;
                            Array.Resize(ref buffer, buffer.Length + (packet.Length - header.DataOffset - (int)packetOffset));
                            Array.Copy(packet, header.DataOffset + packetOffset, buffer, oldSize, (packet.Length - header.DataOffset - packetOffset));
                        }

                        // NOTE: do not need to correct for packetOffset here.
                        _NextSequence = header.SequenceNumber + (uint)packet.Length - header.DataOffset;

                        // if PUSH flag is set, return data immedately.
                        // Note: data in the TCP stream can be processed without the PSH flag set, the application must interpret the stream data.
                        if ((header.flags & (byte)TCPFlags.PSH) > 0)
                            break;
                    }
                    else if (header.SequenceNumber > _NextSequence)
                        break;// if the current sequence # is after the last processed packet, stop processing - missing data.  May need recovery in the future.
                }
            }

            // remove any earlier packets from the primary array
            for (int i = Packets.Count - 1; i >= 0; i--)
            {
                if (Utility.ntohl(BitConverter.ToUInt32(Packets[i], 4)) < _NextSequence)
                    Packets.RemoveAt(i);
            }

            if (Packets.Count > 0)
            {
                if (LastPacketTimestamp.AddMilliseconds(2000) < DateTime.UtcNow)
                {
                    Trace.WriteLine("TCPDecoder: >2 sec since last processed packet, resetting stream.", "DEBUG-MACHINA");

                    for (int i = Packets.Count - 1; i >= 0; i--)
                    {
                        // todo: need to explore this logic, can we recover and get next highest sequence?
                        Trace.WriteLine("TCPDecoder: Missing Sequence # [" + _NextSequence.ToString() + "], Dropping packet with sequence # [" +
                            Utility.ntohl(BitConverter.ToUInt32(Packets[i], 4)).ToString() + "].", "DEBUG-MACHINA");
                        Packets.RemoveAt(i);
                    }
                    _NextSequence = 0;
                }
            }

            return buffer;
        }
    }

}

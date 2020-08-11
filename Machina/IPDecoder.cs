// Machina ~ IPDecoder.cs
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
using System.Linq;
using System.Diagnostics;

namespace Machina
{
    /// <summary>
    /// Manages reassembling IP fragments and decoding the IP header.
    /// </summary>
    public class IPDecoder
    {

        // protocol to filter against
        private IPProtocol _protocol;
        // source ip is in network byte order
        private uint _sourceIP;
        // destination ip is in network byte order
        private uint _destinationIP;

        /// <summary>
        /// Collection containing current unprocessed IP fragments
        /// </summary>
        public List<byte[]> Fragments
        { get; }
            = new List<byte[]>();

        /// <summary>
        /// local timestamp of last successfully processed fragment
        /// </summary>
        public DateTime LastFragmentTimestamp
        { get; internal set; }
            = DateTime.MinValue;

        /// <summary>
        /// local timestamp of last fragmented (successful or otherwise) received and passing the filter.
        ///   This can be used to detect network fragmentation issues and present them to the end-user.
        /// </summary>
        public DateTime LastIPFragmentTimestamp
        { get; internal set; }
            = DateTime.MinValue;

        /// <summary>
        /// class constructor
        /// </summary>
        /// <param name="sourceIP">Source IP address to filter packets, in host byte order</param>
        /// <param name="destinationIP">Destination IP address to filter packets, in host byte order</param>
        /// <param name="protocol">Layer-4 Protocol ID to filter packets</param>
        public IPDecoder(uint sourceIP, uint destinationIP, IPProtocol protocol)
        {
            _sourceIP = sourceIP;
            _destinationIP = destinationIP;
            _protocol = protocol;
        }

        /// <summary>
        /// Takes one or more packets as a byte array, validates they match source/destination IP and protocol,
        ///   and stores them for defragmentation processing.
        /// </summary>
        /// <param name="buffer">byte array containing packet data</param>
        /// <param name="size">usable length of byte array - must be less than its allocated length.</param>
        public unsafe void FilterAndStoreData(byte[] buffer, int size)
        {
            int offset = 0;

            if (buffer == null || buffer.Length == 0)
                return;

            if (buffer.Length < size)
            {
                Trace.WriteLine("IPDecoder: Buffer length is less than specified size.  Size=[" + size.ToString() + "], Length=[" + buffer.Length + "]", "DEBUG-MACHINA");
                return;
            }

            fixed (byte* ptr = buffer)
            {
                while (offset < size - sizeof(IPv4Header))
                {
                    // first four bits (network order) of IPv4 and IPv6 is the protocol version
                    byte version = (byte)((ptr + offset)[0] >> 4);
                    if (version == 6)
                    {
                        // TODO: IP6 packets, and mixed IP4/IP6, need to be tested with real-world data.
                        if (offset + sizeof(IPv6Header) > size)
                        {
                            Trace.WriteLine("IPDecoder: IP6 Packet too small for header. offset: " + offset.ToString() + ", size: " + size.ToString(), "DEBUG-MACHINA");
                            return;
                        }

                        IPv6Header header6 = *(IPv6Header*)(ptr + offset);

                        // make sure we have a valid exit condition
                        if (header6.PayloadLength * 8 > buffer.Length - offset - sizeof(IPv6Header))
                        {
                            Trace.WriteLine("IPDecoder: IP6 Packet too small for payload. payload length: " +
                                (header6.payload_length * 8).ToString() + ", Buffer: " + buffer.Length.ToString() + ", offset: " + offset.ToString(), "DEBUG-MACHINA");
                            return;
                        }

                        offset += sizeof(IPv6Header) + (header6.PayloadLength * 8);

                        continue;
                    }
                    else if (version != 4)
                    {
                        Trace.WriteLine("IPDecoder: IP protocol version is neither 4 nor 6. Version is " + version.ToString(), "DEBUG-MACHINA");
                        return;
                    }

                    IPv4Header ip4Header = *(IPv4Header*)(ptr + offset);

                    int packetLength = ip4Header.Length;

                    // work-around for TCP segment offloading
                    if (packetLength == 0 && ip4Header.Id != 0)
                        packetLength = size;

                    // make sure we have a valid exit condition
                    if (packetLength <= 0)
                        return;
                    if (packetLength > 65535)
                    {
                        Trace.WriteLine("IPDecoder: Invalid packet length [" + packetLength.ToString() + "].", "DEBUG-MACHINA");
                        return;
                    }
                    if (packetLength > buffer.Length - offset)
                    {
                        Trace.WriteLine("IPDecoder: buffer too small to hold complete packet.  Packet length is [" + packetLength.ToString() + "], remaining buffer is [" + (buffer.Length - offset).ToString() + "].", "DEBUG-MACHINA");
                        return;
                    }

                    // filter out packets with an incorrect source / destination IP
                    if (_sourceIP == ip4Header.ip_srcaddr &&
                        _destinationIP == ip4Header.ip_destaddr &&
                        _protocol == ip4Header.protocol)
                    {

                        // store payload
                        byte[] ret = new byte[packetLength];
                        Array.Copy(buffer, offset, ret, 0, ret.Length);
                        Fragments.Add(ret);
                    }

                    offset += packetLength;
                }

                // Note: disabled because this seems to occur occasionally for winpcap for no apparent reason.  Packet length is 40 bytes, buffer length is 46.  Eth header is 14 bytes, total payload was 60.
                /*
                if (offset != size)
                {
                    Trace.WriteLine("IPDecoder: Buffer contains extra bytes after processing packets.  Buffer size: [" + size.ToString() + "], final processed length: [" + offset.ToString() + "].");
                    Trace.WriteLine(Utility.ByteArrayToHexString(buffer, 0, size));
                }
                */
            }
        }

        /// <summary>
        /// This returns the next complete payload.  This involves defragmenting any fragments
        /// </summary>
        /// <returns>byte array containing the IP payload.</returns>
        public unsafe byte[] GetNextIPPayload()
        {
            if (Fragments.Count == 0)
                return null;

            List<byte[]> nextFragments;

            // optimize single packet processing
            if (Fragments.Count == 1) 
                nextFragments = Fragments;
            else
                nextFragments = Fragments.OrderBy(x =>
                                    Utility.ntohs(BitConverter.ToUInt16(x, 4))) // identification
                                    .ThenBy(x =>
                                    Utility.ntohs(BitConverter.ToUInt16(x, 6)) & 0x1fff) // fragment offset
                                    .ToList();

            ushort currentId = 0;
            ushort fragmentOffset = 0;
            byte[] payload = null;

            for (int i = 0; i < nextFragments.Count; i++)
            {
                fixed (byte* ptr = nextFragments[i])
                {
                    IPv4Header ip4Header = *(IPv4Header*)ptr;

                    // every new ID resets the internal state - we dont need to return in order.
                    if (currentId != ip4Header.Id)
                    {
                        currentId = ip4Header.Id;
                        payload = null;
                        fragmentOffset = 0;
                    }

                    // skip for now if the offset is incorrect, the correct packet may come soon.
                    if (ip4Header.FragmentOffset == fragmentOffset)
                    {

                        int fragmentDataSize;

                        // correction for fragmented packet
                        // note: ip4Header.length may be zero if using hardwre offloading to network card.
                        if (ip4Header.Length == 0 && ip4Header.Id != 0)
                            fragmentDataSize = nextFragments[i].Length - ip4Header.HeaderLength;
                        else
                            fragmentDataSize = ip4Header.Length - ip4Header.HeaderLength;
                        
                        // resize payload array
                        if (payload == null)
                            payload = new byte[fragmentDataSize];

                        // if this is a fragment, prepare array to accept it and record last fragment time.
                        if (ip4Header.FragmentOffset > 0)
                        {
                            LastIPFragmentTimestamp = DateTime.Now;
                            Array.Resize(ref payload, payload.Length + fragmentDataSize);
                        }

                        // copy packet into payload
                        Array.Copy(nextFragments[i], ip4Header.HeaderLength, payload, fragmentOffset, fragmentDataSize);

                        // add data offset
                        fragmentOffset += (ushort)fragmentDataSize;

                        // return payload if this is the final fragment
                        if ((ip4Header.Flags & (byte)IPFlags.MF) == 0)
                        {
                            // purge current fragments 
                            if (Fragments.Count == 1) // optimize single packet processing
                                Fragments.Clear();
                            else
                            {
                                // remove in reverse order to prevent IEnumerable issues.
                                for (int j = Fragments.Count - 1; j >= 0; j--)
                                {
                                    if (Utility.ntohs((ushort)BitConverter.ToUInt16(Fragments[j], 4)) == currentId)
                                        Fragments.RemoveAt(j);
                                    else if (Utility.ntohs(BitConverter.ToUInt16(Fragments[j], 4)) < currentId - 99)
                                    {
                                        //Trace.WriteLine("IP: Old fragment purged.  Current ID: [" + currentId.ToString("X4") + "], Old ID: + [" +
                                            //IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(Fragments[j], 4)) + "] " + Utility.ByteArrayToHexString(Fragments[j], 0, 50));
                                        Fragments.RemoveAt(j);
                                    }
                                }
                            }

                            return payload;
                        }
                    }
                }
            }

            return null;
        }
    }

}

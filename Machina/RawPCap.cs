// Machina ~ RawPCap.cs
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
using System.Text;

using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Machina
{
    public class RawPCap
    {
        private DeviceState _activeDevice = null;
        private byte[] _buffer = null;

        #region Interop / WinPCap PInvoice
        [StructLayout(LayoutKind.Sequential)]
        private struct pcap_addr
        {
            public IntPtr next; //if not NULL, a pointer to the next element in the list; NULL for the last element of the list 
            public IntPtr addr; //a pointer to a struct sockaddr containing an address 
            public IntPtr netmask; //if not NULL, a pointer to a struct sockaddr that contains the netmask corresponding to the address pointed to by addr. 
            public IntPtr broadaddr; //if not NULL, a pointer to a struct sockaddr that contains the broadcast address corresponding to the address pointed to by addr; may be null if the interface doesn't support broadcasts 
            public IntPtr dstaddr; //if not NULL, a pointer to a struct sockaddr that contains the destination address corresponding to the address pointed to by addr; may be null if the interface isn't a point- to-point interface 
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct pcap_if
        {
            public IntPtr next; //if not NULL, a pointer to the next element in the list; NULL for the last element of the list 
            public string name; //a pointer to a string giving a name for the device to pass to pcap_open_live() 
            public string description; //if not NULL, a pointer to a string giving a human-readable description of the device 
            public IntPtr addresses; //a pointer to the first element of a list of addresses for the interface 
            public uint flags; //PCAP_IF_ interface flags. Currently the only possible flag is PCAP_IF_LOOPBACK, that is set if the interface is a loopback interface
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct sockaddr_in
        {
            public short sin_family;
            public ushort sin_port;
            public uint sin_addr;
            public ulong sin_zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct pcap_pkthdr
        {
            public uint timestamp_sec;
            public uint timestamp_usec;
            public uint caplen; //Length of portion present in the capture. 
            public uint len; //Real length this packet (off wire). 
            public uint npkt; //Ordinal number of the packet (i.e. the first one captured has '1', the second one '2', etc). 
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct bpf_program
        {
            public uint bf_len;
            public IntPtr bf_insns;
        }

        const int PCAP_ERRBUF_SIZE = 256;
        const int PCAP_OPENFLAG_PROMISCUOUS = 1;
        const int KERNEL_BUFFER_SIZE = 1024 * 1024 * 1; // 1MB

        // supported Data Link types, from bpf.h
        const int DLT_EN10MB = 1; // 14-byte header (may also be a 4 byte 802.1Q vlan header!)
        const int DLT_NULL = 0; // 4-byte header

        const int AF_INET = 2; // Address Family IPv4

        /// <summary>
        /// Construct a list of network devices that can be opened with pcap_open_live(). 
        /// </summary>
        /// <param name="alldevsp">a 'struct pcap_if_t' pointer, which will be properly allocated inside this function. When the function returns, it is set to point to the first element of the interface list; each element of the list is of type 'struct pcap_if_t'</param>
        /// <param name="errbuff">a pointer to a user-allocated buffer (of size PCAP_ERRBUF_SIZE) that will contain the error message (in case there is one).</param>
        /// <returns>-1 is returned on failure, in which case errbuf is filled in with an appropriate error message; 0 is returned on success.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_findalldevs(ref IntPtr alldevsp, StringBuilder errbuff);

        /// <summary>
        /// Free an interface list returned by pcap_findalldevs(). 
        /// </summary>
        /// <param name="alldevsp">Pointer to array of devs that was allocated by pcap_findalldevs</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void pcap_freealldevs(IntPtr alldevsp);

        /// <summary>
        /// Open a generic source in order to capture / send (WinPcap only) traffic. 
        /// </summary>
        /// <param name="source">zero-terminated string containing the source name to open. The source name has to include the format prefix according to the new Source Specification Syntax and it cannot be NULL.</param>
        /// <param name="snaplen">length of the packet that has to be retained. For each packet received by the filter, only the first 'snaplen' bytes are stored in the buffer and passed to the user application. For instance, snaplen equal to 100 means that only the first 100 bytes of each packet are stored.</param>
        /// <param name="flags">keeps several flags that can be needed for capturing packets. The allowed flags are defined in the pcap_open() flags </param>
        /// <param name="read_timeout">read timeout in milliseconds. The read timeout is used to arrange that the read not necessarily return immediately when a packet is seen, but that it waits for some amount of time to allow more packets to arrive and to read multiple packets from the OS kernel in one operation. Not all platforms support a read timeout; on platforms that don't, the read timeout is ignored.</param>
        /// <param name="auth">a pointer to a 'struct pcap_rmtauth' that keeps the information required to authenticate the user on a remote machine. In case this is not a remote capture, this pointer can be set to NULL.</param>
        /// <param name="errbuff">a pointer to a user-allocated buffer which will contain the error in case this function fails</param>
        /// <returns>A pointer to a 'pcap_t' which can be used as a parameter to the following calls (pcap_compile() and so on) and that specifies an opened WinPcap session. In case of problems, it returns NULL and the 'errbuf' variable keeps the error message.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr pcap_open(string source, int snaplen, int flags, int read_timeout, IntPtr auth, StringBuilder errbuff);

        /// <summary>
        /// Return the link layer of an adapter. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <returns>returns the link layer type</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_datalink(IntPtr p);

        /// <summary>
        /// Set the size of the kernel buffer associated with an adapter. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="dim">dim specifies the size of the buffer in bytes.</param>
        /// <returns>The return value is 0 when the call succeeds, -1 otherwise.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_setbuff(IntPtr p, int dim);

        /// <summary>
        /// Compile a packet filter, converting an high level filtering expression (see Filtering expression syntax) in a program that can be interpreted by the kernel-level filtering engine. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="fp">A pointer to a bpf_program struct and is filled in by pcap_compile()</param>
        /// <param name="str">filtering expression </param>
        /// <param name="optimize">optimize controls whether optimization on the resulting code is performed</param>
        /// <param name="netmask"> netmask specifies the IPv4 netmask of the network on which packets are being captured; it is used only when checking for IPv4 broadcast addresses in the filter program. If the netmask of the network on which packets are being captured isn't known to the program, or if packets are being captured on the Linux "any" pseudo-interface that can capture on more than one network, a value of 0 can be supplied; tests for IPv4 broadcast addreses won't be done correctly, but all other tests in the filter program will be OK.</param>
        /// <returns>A return of -1 indicates an error in which case pcap_geterr() may be used to display the error text.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_compile(IntPtr p, IntPtr fp, string str, int optimize, uint netmask);

        /// <summary>
        /// Associate a filter to a capture. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="fp">fp is a pointer to a bpf_program struct, usually the result of a call to pcap_compile()</param>
        /// <returns>-1 is returned on failure, in which case pcap_geterr() may be used to display the error text; 0 is returned on success.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_setfilter(IntPtr p, IntPtr fp);


        /// <summary>
        /// pcap_freecode() is used to free up allocated memory pointed to by a bpf_program struct generated by pcap_compile() when that BPF program is no longer needed, for example after it has been made the filter program for a pcap structure by a call to pcap_setfilter().
        /// </summary>
        /// <param name="fp">bpf_program struct generated by pcap_compile()</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void pcap_freecode(IntPtr fp);

        /// <summary>
        /// Read a packet from an interface or from an offline capture.
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="pkt_header">Pointer to the header of the next captured packet</param>
        /// <param name="pkt_data">Pointer to the data of the next captured packet</param>
        /// <returns>1 if the packet has been read without problems
        ///          0 if the timeout set with pcap_open_live() has elapsed. In this case pkt_header and pkt_data don't point to a valid packet
        ///         -1 if an error occurred
        ///         -2 if EOF was reached reading from an offline capture</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int pcap_next_ex(IntPtr p, ref IntPtr pkt_header, ref IntPtr pkt_data);

        /// <summary>
        /// close the files associated with p and deallocates resources. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void pcap_close(IntPtr p);

        #endregion

        #region Device Structs

        private class Device
        {
            public string Name { get; internal set; }
            public string Description { get; internal set; }
            public List<uint> Addresses { get; internal set; }
        }

        private class DeviceState
        {
            public Device Device { get; internal set; }
            public int LinkType { get; internal set; }
            public IntPtr Handle { get; internal set; }
        }
        #endregion

        public void Create(uint address)
        {
            _buffer = new byte[1024 * 128];

            Device device = GetAllDevices().FirstOrDefault(x =>
                x.Addresses.Contains(address));

            if (!string.IsNullOrWhiteSpace(device.Name))
                StartCapture(device);
            else
                Trace.WriteLine("IP [" + new System.Net.IPAddress(address).ToString() + " selected but unable to find corresponding WinPCap device.");
        }

        public void Destroy()
        {
            try
            {
                if (_activeDevice == null)
                    return;

                if (_activeDevice.Handle != IntPtr.Zero)
                    pcap_close(_activeDevice.Handle);

                _activeDevice.Handle = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception cleaning up RawPCap class. " + ex.ToString());
            }
        }

        public unsafe int Receive(out byte[] buffer)
        {
            buffer = _buffer;

            if (_activeDevice == null)
                return 0;

            try
            {
                IntPtr packetDataPtr = IntPtr.Zero;
                IntPtr packetHeaderPtr = IntPtr.Zero;

                int layer2Length = (_activeDevice.LinkType == DLT_EN10MB ? 14 : 4); // 14 for ethernet, 4 for loopback

                // note: buffer returned by pcap_next_ex is static and owned by pcap library, does not need to be freed.
                int status = pcap_next_ex(_activeDevice.Handle, ref packetHeaderPtr, ref packetDataPtr);
                if (status < 0)
                {
                    // todo: log?
                    return 0;
                }
                else if (status != 0)
                {
                    pcap_pkthdr packetHeader = *(pcap_pkthdr*)packetHeaderPtr;

                    if (packetHeader.caplen <= layer2Length)
                        return 0;

                    // prepare data - skip the 14-byte ethernet header
                    int size = (int)packetHeader.caplen - layer2Length;
                    if (size > _buffer.Length)
                        throw new ApplicationException("packet length too large: " + size.ToString());

                    Marshal.Copy(packetDataPtr + layer2Length, _buffer, 0, size);

                    return size;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception during WinPCap Receive. " + ex.ToString());
            }

            return 0;
        }


        private unsafe IList<Device> GetAllDevices()
        {
            List<Device> deviceList = new List<Device>();
            IntPtr deviceListPtr = IntPtr.Zero;
            IntPtr currentAddress;

            try
            {
                StringBuilder errorBuffer = new StringBuilder(PCAP_ERRBUF_SIZE);
                int returnCode = pcap_findalldevs(ref deviceListPtr, errorBuffer);
                if (returnCode != 0)
                    throw new ApplicationException("Cannot enumerate devices: [" + errorBuffer.ToString() + "].");

                IntPtr ip = deviceListPtr;
                while (ip != IntPtr.Zero)
                {
                    pcap_if dev = (pcap_if)Marshal.PtrToStructure(ip, typeof(pcap_if));

                    Device device = new Device();
                    device.Name = dev.name;
                    device.Description = dev.description;
                    device.Addresses = new List<uint>();
                    currentAddress = dev.addresses;

                    while (currentAddress != IntPtr.Zero)
                    {
                        pcap_addr address = *(pcap_addr*)currentAddress;

                        if (address.addr != IntPtr.Zero)
                        {
                            sockaddr_in sockaddress = *(sockaddr_in*)address.addr;
                            if (sockaddress.sin_family == AF_INET)
                                device.Addresses.Add(sockaddress.sin_addr);
                        }

                        currentAddress = address.next;
                    }

                    deviceList.Add(device);

                    ip = dev.next;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to get WinPcap device list.", ex);
            }
            finally
            {
                // always release memory after getting device list.
                if (deviceListPtr != IntPtr.Zero)
                    pcap_freealldevs(deviceListPtr);
            }

            return deviceList;
        }

        private void StartCapture(Device device)
        {
            const string filterText = "ip and tcp";

            IntPtr filter = Marshal.AllocHGlobal(12);

            try
            {
                if (_activeDevice != null)
                    Destroy();

                _activeDevice = new DeviceState()
                {
                    Device = device,
                    Handle = IntPtr.Zero
                };

                StringBuilder errorBuffer = new StringBuilder(PCAP_ERRBUF_SIZE);

                // flags=0 turns off promiscous mode, which is not needed or desired.
                _activeDevice.Handle = pcap_open(device.Name, 65536, 0, 500, IntPtr.Zero, errorBuffer);
                if (_activeDevice.Handle == IntPtr.Zero)
                    throw new ApplicationException("Cannot open pcap interface [" + device.Name + "].  Error: " + errorBuffer.ToString());

                // check data link type
                _activeDevice.LinkType = pcap_datalink(_activeDevice.Handle);
                if (_activeDevice.LinkType != DLT_EN10MB && _activeDevice.LinkType != DLT_NULL)
                    throw new ApplicationException("Interface [" + device.Description + "] does not appear to support Ethernet.");

                // create filter
                if (pcap_compile(_activeDevice.Handle, filter, filterText, 1, 0) != 0)
                    throw new ApplicationException("Unable to create TCP packet filter.");

                // apply filter
                if (pcap_setfilter(_activeDevice.Handle, filter) != 0)
                    throw new ApplicationException("Unable to apply TCP packet filter.");

                // free filter memory
                pcap_freecode(filter);
            }
            catch (Exception ex)
            {
                // clean up device
                if (_activeDevice.Handle != IntPtr.Zero)
                    pcap_close(_activeDevice.Handle);

                _activeDevice = null;

                throw new ApplicationException("Unable to open winpcap device [" + device.Name + "].", ex);
            }
            finally
            {
                // free memory
                Marshal.FreeHGlobal(filter);
            }
        }

    }
}

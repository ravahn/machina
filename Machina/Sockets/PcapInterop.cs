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
using System.Runtime.InteropServices;
using System.Text;

namespace Machina.Sockets
{
    internal static class PcapInterop
    {
        #region Interop / WinPCap PInvoice
#pragma warning disable CA1707 // Identifiers should not contain underscores
        [StructLayout(LayoutKind.Sequential)]
        internal struct pcap_addr
        {
            public IntPtr next; //if not NULL, a pointer to the next element in the list; NULL for the last element of the list 
            public IntPtr addr; //a pointer to a struct sockaddr containing an address 
            public IntPtr netmask; //if not NULL, a pointer to a struct sockaddr that contains the netmask corresponding to the address pointed to by addr. 
            public IntPtr broadaddr; //if not NULL, a pointer to a struct sockaddr that contains the broadcast address corresponding to the address pointed to by addr; may be null if the interface doesn't support broadcasts 
            public IntPtr dstaddr; //if not NULL, a pointer to a struct sockaddr that contains the destination address corresponding to the address pointed to by addr; may be null if the interface isn't a point- to-point interface 
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct pcap_if
        {
            public IntPtr next; //if not NULL, a pointer to the next element in the list; NULL for the last element of the list 
            public string name; //a pointer to a string giving a name for the device to pass to pcap_open_live() 
            public string description; //if not NULL, a pointer to a string giving a human-readable description of the device 
            public IntPtr addresses; //a pointer to the first element of a list of addresses for the interface 
            public uint flags; //PCAP_IF_ interface flags. Currently the only possible flag is PCAP_IF_LOOPBACK, that is set if the interface is a loopback interface
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct sockaddr_in
        {
            public short sin_family;
            public ushort sin_port;
            public uint sin_addr;
            public ulong sin_zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct pcap_pkthdr
        {
            public uint timestamp_sec;
            public uint timestamp_usec;
            public uint caplen; //Length of portion present in the capture. 
            public uint len; //Real length this packet (off wire). 
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct bpf_program
        {
            public uint bf_len;
            public IntPtr bf_insns;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct pcap_rmtauth
        {
            public int type;
            public string username;
            public string password;
        }

        internal const int PCAP_BUF_SIZE = 1024;
        internal const int PCAP_ERRBUF_SIZE = 256;
        internal const int PCAP_OPENFLAG_PROMISCUOUS = 1;
        internal const int PCAP_OPENFLAG_NOCAPTURE_RPCAP = 4;
        internal const int PCAP_OPENFLAG_MAX_RESPONSIVENESS = 16;
        internal const int RPCAP_RMTAUTH_NULL = 0;
        internal const int RPCAP_RMTAUTH_PWD = 1;

        internal const int KERNEL_BUFFER_SIZE = 1024 * 1024 * 1; // 1MB

        // supported Data Link types, from bpf.h
        internal const int DLT_EN10MB = 1; // 14-byte header (may also be a 4 byte 802.1Q vlan header!)
        internal const int DLT_RAW = 12; // no header
        internal const int DLT_NULL = 0; // 4-byte header

        internal const int AF_INET = 2; // Address Family IPv4
        internal const int AF_INET_BSD = 528; // Address Family IPv4 for BSD kernels

        /// <summary>
        /// Create a list of network devices that can be opened with pcap_open().
        /// </summary>
        /// <param name="source">a char* buffer that keeps the 'source localtion', according to the new WinPcap syntax. This source will be examined looking for adapters (local or remote) (e.g. source can be 'rpcap://' for local adapters or 'rpcap://host:port' for adapters on a remote host) or pcap files (e.g. source can be 'file://c:/myfolder/'). The strings that must be prepended to the 'source' in order to define if we want local/remote adapters or files is defined in the new Source Specification Syntax.</param>
        /// <param name="auth">a pointer to a pcap_rmtauth structure. This pointer keeps the information required to authenticate the RPCAP connection to the remote host. This parameter is not meaningful in case of a query to the local host: in that case it can be NULL.</param>
        /// <param name="alldevsp">a 'struct pcap_if_t' pointer, which will be properly allocated inside this function. When the function returns, it is set to point to the first element of the interface list; each element of the list is of type 'struct pcap_if_t'</param>
        /// <param name="errbuff">a pointer to a user-allocated buffer (of size PCAP_ERRBUF_SIZE) that will contain the error message (in case there is one).</param>
        /// <returns>-1 is returned on failure, in which case errbuf is filled in with an appropriate error message; 0 is returned on success.</returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        internal static extern int pcap_findalldevs_ex(string source, ref pcap_rmtauth auth, ref IntPtr alldevsp, StringBuilder errbuff);

        /// <summary>
        /// Free an interface list returned by pcap_findalldevs(). 
        /// </summary>
        /// <param name="alldevsp">Pointer to array of devs that was allocated by pcap_findalldevs</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pcap_freealldevs(IntPtr alldevsp);

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
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        internal static extern IntPtr pcap_open(string source, int snaplen, int flags, int read_timeout, ref pcap_rmtauth auth, StringBuilder errbuff);

        /// <summary>
        /// Return the link layer of an adapter. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <returns>returns the link layer type</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pcap_datalink(IntPtr p);

        /// <summary>
        /// Set the size of the kernel buffer associated with an adapter. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="dim">dim specifies the size of the buffer in bytes.</param>
        /// <returns>The return value is 0 when the call succeeds, -1 otherwise.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pcap_setbuff(IntPtr p, int dim);

        /// <summary>
        /// Compile a packet filter, converting an high level filtering expression (see Filtering expression syntax) in a program that can be interpreted by the kernel-level filtering engine. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="fp">A pointer to a bpf_program struct and is filled in by pcap_compile()</param>
        /// <param name="str">filtering expression </param>
        /// <param name="optimize">optimize controls whether optimization on the resulting code is performed</param>
        /// <param name="netmask"> netmask specifies the IPv4 netmask of the network on which packets are being captured; it is used only when checking for IPv4 broadcast addresses in the filter program. If the netmask of the network on which packets are being captured isn't known to the program, or if packets are being captured on the Linux "any" pseudo-interface that can capture on more than one network, a value of 0 can be supplied; tests for IPv4 broadcast addreses won't be done correctly, but all other tests in the filter program will be OK.</param>
        /// <returns>A return of -1 indicates an error in which case pcap_geterr() may be used to display the error text.</returns>
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        internal static extern int pcap_compile(IntPtr p, ref bpf_program fp, string str, int optimize, uint netmask);

        /// <summary>
        /// Associate a filter to a capture. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        /// <param name="fp">fp is a pointer to a bpf_program struct, usually the result of a call to pcap_compile()</param>
        /// <returns>-1 is returned on failure, in which case pcap_geterr() may be used to display the error text; 0 is returned on success.</returns>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int pcap_setfilter(IntPtr p, ref bpf_program fp);


        /// <summary>
        /// pcap_freecode() is used to free up allocated memory pointed to by a bpf_program struct generated by pcap_compile() when that BPF program is no longer needed, for example after it has been made the filter program for a pcap structure by a call to pcap_setfilter().
        /// </summary>
        /// <param name="fp">bpf_program struct generated by pcap_compile()</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pcap_freecode(ref bpf_program fp);

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
        internal static extern int pcap_next_ex(IntPtr p, ref IntPtr pkt_header, ref IntPtr pkt_data);

        /// <summary>
        /// close the files associated with p and deallocates resources. 
        /// </summary>
        /// <param name="p">A pointer to a 'pcap_t' instance previously returned by pcap_open*</param>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pcap_close(IntPtr p);

        /// <summary>
        /// returns the most recent error.
        /// </summary>
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr pcap_geterr(IntPtr ph);

        #endregion
    }
}

// Machina ~ WinPcapWrapper.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NLog;

namespace Machina
{
    public static class WinPcapWrapper
    {
        private const int PCAP_ERRBUF_SIZE = 256;
        private const int PCAP_OPENFLAG_PROMISCUOUS = 1;
        private const int KERNEL_BUFFER_SIZE = 1048576;
        private const int DLT_EN10MB = 1;
        private const int DLT_NULL = 0;
        private const int AF_INET = 2;

        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private static ConcurrentDictionary<string, DeviceState> _activeDevices;
        private static EventHandler<DataReceivedEventArgs> _DataReceived;

        static WinPcapWrapper()
        {
            _activeDevices = new ConcurrentDictionary<string, DeviceState>();
        }

        public static event EventHandler<DataReceivedEventArgs> DataReceived
        {
            add
            {
                var dataReceived = _DataReceived;
                EventHandler<DataReceivedEventArgs> comparand;
                do
                {
                    comparand = dataReceived;
                    var eventHandler = (EventHandler<DataReceivedEventArgs>) Delegate.Combine(comparand, value);
                    dataReceived = Interlocked.CompareExchange(ref _DataReceived, eventHandler, comparand);
                }
                while (dataReceived != comparand);
            }
            remove
            {
                var dataReceived = _DataReceived;
                EventHandler<DataReceivedEventArgs> comparand;
                do
                {
                    comparand = dataReceived;
                    var eventHandler = (EventHandler<DataReceivedEventArgs>) Delegate.Remove(comparand, value);
                    dataReceived = Interlocked.CompareExchange(ref _DataReceived, eventHandler, comparand);
                }
                while (dataReceived != comparand);
            }
        }

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_findalldevs(ref IntPtr alldevsp, StringBuilder errbuff);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pcap_freealldevs(IntPtr alldevsp);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pcap_open(string source, int snaplen, int flags, int read_timeout, IntPtr auth, StringBuilder errbuff);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_datalink(IntPtr p);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_setbuff(IntPtr p, int dim);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_compile(IntPtr p, IntPtr fp, string str, int optimize, uint netmask);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_setfilter(IntPtr p, IntPtr fp);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pcap_freecode(IntPtr fp);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pcap_next_ex(IntPtr p, ref IntPtr pkt_header, ref IntPtr pkt_data);

        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void pcap_close(IntPtr p);

        private static void OnDataReceived(DataReceivedEventArgs e)
        {
            var eventHandler = _DataReceived;
            if (eventHandler == null)
            {
                return;
            }
            eventHandler(null, e);
        }

        public static IList<Device> GetAllDevices()
        {
            var devices = new List<Device>();
            var alldevsp = IntPtr.Zero;
            try
            {
                var errbuff = new StringBuilder(256);
                if (pcap_findalldevs(ref alldevsp, errbuff) != 0)
                {
                    throw new ApplicationException($"Cannot Enumerate Devices: [{errbuff}]");
                }
                pcap_if pcapIf;
                for (var ptr1 = alldevsp; ptr1 != IntPtr.Zero; ptr1 = pcapIf.next)
                {
                    pcapIf = (pcap_if) Marshal.PtrToStructure(ptr1, typeof(pcap_if));
                    var device = new Device();
                    device.Name = pcapIf.name;
                    device.Description = pcapIf.description;
                    device.Addresses = new List<string>();
                    pcap_addr pcapAddr;
                    for (var ptr2 = pcapIf.addresses; ptr2 != IntPtr.Zero; ptr2 = pcapAddr.next)
                    {
                        pcapAddr = (pcap_addr) Marshal.PtrToStructure(ptr2, typeof(pcap_addr));
                        if (pcapAddr.addr != IntPtr.Zero)
                        {
                            var sockaddrIn = (sockaddr_in) Marshal.PtrToStructure(pcapAddr.addr, typeof(sockaddr_in));
                            if (sockaddrIn.sin_family == 2)
                            {
                                device.Addresses.Add($"{sockaddrIn.sin_addr[0]}.{sockaddrIn.sin_addr[1]}.{sockaddrIn.sin_addr[2]}.{sockaddrIn.sin_addr[3]}");
                            }
                        }
                    }
                    devices.Add(device);
                }
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, new ApplicationException("Unable To Get WinPcap Device List", ex));
            }
            finally
            {
                if (alldevsp != IntPtr.Zero)
                {
                    pcap_freealldevs(alldevsp);
                }
            }
            return devices;
        }

        public static void StartCapture(SocketObject state)
        {
            var deviceState = new DeviceState();

            lock (deviceState)
            {
                deviceState.Device = state.Device;
                deviceState.Handle = IntPtr.Zero;
                deviceState.Cancel = false;
                deviceState.State = state;
                var num = Marshal.AllocHGlobal(12);
                try
                {
                    if (_activeDevices.ContainsKey(state.Device.Name))
                    {
                        StopCapture(state.Device.Name);
                    }
                    var errbuff = new StringBuilder(256);
                    deviceState.Handle = pcap_open(state.Device.Name, 65536, 0, 500, IntPtr.Zero, errbuff);
                    if (deviceState.Handle == IntPtr.Zero)
                    {
                        throw new ApplicationException($"Cannot Open PCap Interface [{state.Device.Name}] => Error: {errbuff}");
                    }
                    deviceState.LinkType = pcap_datalink(deviceState.Handle);
                    if (deviceState.LinkType != 1 && deviceState.LinkType != 0)
                    {
                        throw new ApplicationException($"Interface [{state.Device.Description}] Does Not Appear To Support Ethernet");
                    }
                    if (pcap_compile(deviceState.Handle, num, "ip and tcp", 1, 0U) != 0)
                    {
                        throw new ApplicationException("Unable To Create TCP Packet Filter");
                    }
                    if (pcap_setfilter(deviceState.Handle, num) != 0)
                    {
                        throw new ApplicationException("Unable To Apply TCP Packet Filter");
                    }
                    pcap_freecode(num);
                    _activeDevices[state.Device.Name] = deviceState;
                }
                catch (Exception ex)
                {
                    if (deviceState.Handle != IntPtr.Zero)
                    {
                        pcap_close(deviceState.Handle);
                    }
                    NetworkHandler.Instance.RaiseException(Logger, new ApplicationException($"Unable To Open WinPCap Device [{state.Device.Name}]", ex));
                }
                finally
                {
                    Marshal.FreeHGlobal(num);
                }
                ThreadPool.QueueUserWorkItem(PollNetworkDevice, _activeDevices[state.Device.Name]);
            }
        }

        public static void StopCapture(string deviceName)
        {
            try
            {
                if (!_activeDevices.ContainsKey(deviceName))
                {
                    return;
                }
                var deviceState = _activeDevices[deviceName];

                lock (deviceState)
                {
                    deviceState.Cancel = true;
                    Thread.Sleep(500);
                    if (deviceState.Handle != IntPtr.Zero)
                    {
                        pcap_close(deviceState.Handle);
                    }
                    deviceState.Handle = IntPtr.Zero;
                    DeviceState removed;
                    _activeDevices.TryRemove(deviceName, out removed);
                }
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, ex);
            }
        }

        public static void StopAllCapture()
        {
            var values = _activeDevices.Values;
            foreach (var deviceName in values.Select(x => x.Device.Name)
                                             .Where(x => x != string.Empty))
            {
                StopCapture(deviceName);
            }
        }

        private static void PollNetworkDevice(object stateInfo)
        {
            var deviceState = (DeviceState) stateInfo;
            var offset = deviceState.LinkType == 1 ? 14 : 4;
            var packetData = IntPtr.Zero;
            var packetHeader = IntPtr.Zero;
            while (!deviceState.Cancel)
            {
                var packet = pcap_next_ex(deviceState.Handle, ref packetHeader, ref packetData);
                if (packet < 0)
                {
                    break;
                }
                if (packet == 0)
                {
                    Thread.Sleep(10);
                }
                else
                {
                    var pcapPkthdr = (pcap_pkthdr) Marshal.PtrToStructure(packetHeader, typeof(pcap_pkthdr));
                    if (pcapPkthdr.caplen > offset)
                    {
                        var destination = new byte[pcapPkthdr.caplen - offset];
                        Marshal.Copy(IntPtr.Add(packetData, offset), destination, 0, (int) pcapPkthdr.caplen - offset);
                        var eventArgs = new DataReceivedEventArgs();
                        eventArgs.Data = destination;
                        eventArgs.Device = deviceState;
                        OnDataReceived(eventArgs);
                    }
                    else
                    {
                        continue;
                    }
                }
                packetHeader = IntPtr.Zero;
                packetData = IntPtr.Zero;
                if (deviceState.Cancel)
                {
                    break;
                }
            }
        }

        private struct pcap_addr
        {
            public IntPtr addr;
            public IntPtr broadaddr;
            public IntPtr dstaddr;
            public IntPtr netmask;
            public IntPtr next;
        }

        private struct pcap_if
        {
            public IntPtr addresses;
            public string description;
            public uint flags;
            public string name;
            public IntPtr next;
        }

        private struct sockaddr_in
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] sin_addr;

            public short sin_family;
            public ushort sin_port;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] sin_zero;
        }

        private struct pcap_pkthdr
        {
            public uint caplen;
            public uint len;
            public uint npkt;
            public uint timestamp_sec;
            public uint timestamp_usec;
        }

        private struct bpf_program
        {
            public IntPtr bf_insns;
            public uint bf_len;
        }

        public struct Device
        {
            public string Name { get; internal set; }
            public string Description { get; internal set; }
            public List<string> Addresses { get; internal set; }
        }

        public class DeviceState
        {
            public SocketObject State { get; internal set; }
            public Device Device { get; internal set; }
            public int LinkType { get; internal set; }
            public IntPtr Handle { get; internal set; }
            public bool Cancel { get; internal set; }
        }

        public class DataReceivedEventArgs : EventArgs
        {
            public byte[] Data { get; set; }
            public DeviceState Device { get; set; }
        }
    }
}

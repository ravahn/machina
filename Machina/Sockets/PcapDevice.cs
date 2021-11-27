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
using System.Runtime.InteropServices;
using System.Text;
using static Machina.Sockets.PcapInterop;

namespace Machina.Sockets
{
    internal class PcapDevice
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public IList<uint> Addresses { get; internal set; }

        public static unsafe IList<PcapDevice> GetAllDevices(string source, ref pcap_rmtauth auth)
        {
            List<PcapDevice> deviceList = new List<PcapDevice>();
            IntPtr deviceListPtr = IntPtr.Zero;
            IntPtr currentAddress;

            try
            {
                StringBuilder errorBuffer = new StringBuilder(PCAP_ERRBUF_SIZE);
                int returnCode = pcap_findalldevs_ex(source, ref auth, ref deviceListPtr, errorBuffer);
                if (returnCode != 0)
                    throw new PcapException($"Cannot enumerate devices: [{errorBuffer}].");

                IntPtr ip = deviceListPtr;
                while (ip != IntPtr.Zero)
                {
                    pcap_if dev = (pcap_if)Marshal.PtrToStructure(ip, typeof(pcap_if));

                    PcapDevice device = new PcapDevice
                    {
                        Name = dev.name,
                        Description = dev.description,
                        Addresses = new List<uint>()
                    };
                    currentAddress = dev.addresses;

                    while (currentAddress != IntPtr.Zero)
                    {
                        pcap_addr address = *(pcap_addr*)currentAddress;

                        if (address.addr != IntPtr.Zero)
                        {
                            sockaddr_in sockaddress = *(sockaddr_in*)address.addr;
                            if (sockaddress.sin_family == AF_INET || sockaddress.sin_family == AF_INET_BSD)
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
                throw new PcapException("Unable to get WinPcap device list.", ex);
            }
            finally
            {
                // always release memory after getting device list.
                if (deviceListPtr != IntPtr.Zero)
                    pcap_freealldevs(deviceListPtr);
            }

            return deviceList;
        }

    }
}

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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Machina.Infrastructure
{
    public static class InterfaceHelper
    {

        public static string GetLocalIPv4(NetworkInterfaceType type = NetworkInterfaceType.Ethernet)
        {
            // Repurposed from: http://stackoverflow.com/a/28621250/2685650.

            return NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(ni =>
                    ni.NetworkInterfaceType == type
                    && ni.OperationalStatus == OperationalStatus.Up
                    && ni.GetIPProperties().GatewayAddresses.FirstOrDefault() != null
                    && ni.GetIPProperties().UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork) != null
                )
                ?.GetIPProperties()
                .UnicastAddresses
                .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address
                ?.ToString()
                ?? string.Empty;
        }

        public static IPAddress[] GetAllIPAddresses()
        {
            List<NetworkInterface> _interfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();

            IPAddress[] ret = _interfaces
                .Where(x => x.OperationalStatus == OperationalStatus.Up)
                .Select(x => x.GetIPProperties())
                .SelectMany(x => x.UnicastAddresses)
                .Select(x => x.Address)
                .Where(x => x.IsIPv6LinkLocal == false)
                .Where(x => (x.ToString() ?? "").Contains('.'))
                .ToArray();

            return ret;
        }


        public static IList<string> GetNetworkInterfaceIPs()
        {
            List<string> ret = new List<string>();

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                if (interfaces[i].OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties ipProps = interfaces[i].GetIPProperties();
                for (int j = 0; j < ipProps.UnicastAddresses.Count; j++)
                {
                    string ip = ipProps.UnicastAddresses[j]?.Address?.ToString() ?? "";
                    if (ip.Length <= 15 && ip.Contains('.')) // ipv4 addresses only
                        if (!ret.Any(x => x == ip))
                            ret.Add(ip);
                }
            }

            return ret;
        }
    }
}

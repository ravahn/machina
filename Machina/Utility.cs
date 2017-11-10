// Machina ~ Utility.cs
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
using System.Net.NetworkInformation;
using System.Text;

namespace Machina
{
    public class Utility
    {
        public static string ByteArrayToHexString(byte[] data)
        {
            return ByteArrayToHexString(data, 0, data.Length);
        }

        public static string ByteArrayToHexString(byte[] data, int offset)
        {
            return ByteArrayToHexString(data, offset, data.Length);
        }

        public static string ByteArrayToHexString(byte[] data, int offset, int size)
        {
            if (data == null || data.Length == 0)
                return "";
            if (offset + size > data.Length)
                size = data.Length - offset;
            if (size <= 0)
                return "";

            StringBuilder sb = new StringBuilder(size * 2);

            for (int i = offset; i < offset + size; i++)
                sb.Append(data[i].ToString("X2"));

            return sb.ToString();
        }

        public static byte[] HexStringToByteArray(string data)
        {
            data = data.Replace(Environment.NewLine, "");
            byte[] ret = new byte[data.Length / 2];

            for (int i = 0; i < data.Length / 2; i++)
            {
                ret[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);
            }

            return ret;
        }

        private static readonly DateTime MinEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime EpochToDateTime(long epoch)
        {
            return MinEpoch.AddMilliseconds(epoch);
        }

        public static ushort ntohs(ushort value)
        {
            return (ushort)System.Net.IPAddress.NetworkToHostOrder((short)value);
        }

        public static uint ntohl(uint value)
        {
            return (uint)System.Net.IPAddress.NetworkToHostOrder((int)value);
        }

        public static ulong ntohq(ulong value)
        {
            return (ulong)System.Net.IPAddress.NetworkToHostOrder((long)value);
        }

        public static ushort htons(ushort value)
        {
            return (ushort)System.Net.IPAddress.HostToNetworkOrder((short)value);
        }

        public static uint htonl(uint value)
        {
            return (uint)System.Net.IPAddress.HostToNetworkOrder((int)value);
        }

        public static ulong htonq(ulong value)
        {
            return (ulong)System.Net.IPAddress.HostToNetworkOrder((long)value);
        }

        public static List<string> GetNetworkInterfaceIPs()
        {
            List<string> ret = new List<string>();

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (string ip in ipProps.UnicastAddresses.Select(x => x.Address.ToString() ?? ""))
                    if (ip.Length <= 15 && ip.Contains('.')) // ipv4 addresses only
                        if (!ret.Any(x => x == ip))
                            ret.Add(ip);
            }

            return ret;
        }
    }
}


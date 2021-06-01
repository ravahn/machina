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
using System.Globalization;
using System.Net;
using System.Text;

namespace Machina.Infrastructure
{
    public class ConversionUtility
    {

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
                _ = sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));

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
            return (ushort)IPAddress.NetworkToHostOrder((short)value);
        }

        public static uint ntohl(uint value)
        {
            return (uint)IPAddress.NetworkToHostOrder((int)value);
        }

        public static ulong ntohq(ulong value)
        {
            return (ulong)IPAddress.NetworkToHostOrder((long)value);
        }

        public static ushort htons(ushort value)
        {
            return (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public static uint htonl(uint value)
        {
            return (uint)IPAddress.HostToNetworkOrder((int)value);
        }

        public static ulong htonq(ulong value)
        {
            return (ulong)IPAddress.HostToNetworkOrder((long)value);
        }

        public static uint IPStringToUint(string ip)
        {
            if (!IPAddress.TryParse(ip, out IPAddress address))
                return 0;
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return 0;
            uint longIp = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            return longIp;
        }

    }
}


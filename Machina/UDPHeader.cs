// Machina ~ UDPHeader.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using NLog;

namespace Machina
{
    public class UDPHeader
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private readonly short _checksum;
        private readonly List<byte> _data = new List<byte>();
        private readonly ushort _destinationPort;
        private readonly ushort _length;
        private readonly ushort _sourcePort;

        public UDPHeader(byte[] byBuffer, int nReceived)
        {
            try
            {
                using (var memoryStream = new MemoryStream(byBuffer, 0, nReceived))
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        _sourcePort = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _destinationPort = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _length = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _checksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        var tempData = new byte[nReceived - 8];
                        Array.Copy(byBuffer, 8, tempData, 0, nReceived - 8);
                        foreach (var b in tempData)
                        {
                            _data.Add(b);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, ex);
            }
        }

        public string SourcePort
        {
            get { return _sourcePort.ToString(CultureInfo.InvariantCulture); }
        }

        public string DestinationPort
        {
            get { return _destinationPort.ToString(CultureInfo.InvariantCulture); }
        }

        public string Length
        {
            get { return _length.ToString(CultureInfo.InvariantCulture); }
        }

        public string Checksum
        {
            get { return $"0x{_checksum:x2}"; }
        }

        public List<byte> Data
        {
            get { return _data; }
        }
    }
}

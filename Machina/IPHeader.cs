// Machina ~ IPHeader.cs
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
    public class IPHeader
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private readonly short _checksum;
        private readonly List<byte> _data = new List<byte>();
        private readonly uint _destinationIPAddress;
        private readonly byte _differentiatedServices;
        private readonly ushort _flags;
        private readonly byte _headerLength;
        private readonly ushort _identification;
        private readonly byte _protocol;
        private readonly uint _sourceIPAddress;
        private readonly ushort _totalLength;
        private readonly byte _TTL;
        private readonly byte _versionAndHeaderLength;

        public IPHeader(byte[] byBuffer, int nReceived)
        {
            try
            {
                using (var memoryStream = new MemoryStream(byBuffer, 0, nReceived))
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        _versionAndHeaderLength = binaryReader.ReadByte();
                        _differentiatedServices = binaryReader.ReadByte();
                        _totalLength = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _identification = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _flags = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _TTL = binaryReader.ReadByte();
                        _protocol = binaryReader.ReadByte();
                        _checksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _sourceIPAddress = (uint) binaryReader.ReadInt32();
                        _destinationIPAddress = (uint) binaryReader.ReadInt32();
                        _headerLength = _versionAndHeaderLength;
                        _headerLength <<= 4;
                        _headerLength >>= 4;
                        _headerLength *= 4;
                        if (_totalLength - _headerLength > 0)
                        {
                            var tempData = new byte[_totalLength - _headerLength];
                            Array.Copy(byBuffer, _headerLength, tempData, 0, _totalLength - _headerLength);
                            foreach (var b in tempData)
                            {
                                _data.Add(b);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, ex);
            }
        }

        public string Version
        {
            get
            {
                switch (_versionAndHeaderLength >> 4)
                {
                    case 4: return "IP v4";
                    case 6: return "IP v6";
                    default: return "Unknown";
                }
            }
        }

        public string HeaderLength
        {
            get { return _headerLength.ToString(); }
        }

        public ushort MessageLength
        {
            get { return (ushort) (_totalLength - _headerLength); }
        }

        public string DifferentiatedServices
        {
            get { return $"0x{_differentiatedServices:x2} ({_differentiatedServices})"; }
        }

        public string Flags
        {
            get
            {
                var newFlags = _flags >> 13;
                switch (newFlags)
                {
                    case 2: return "Don't fragment";
                    case 1: return "More fragments to come";
                    default: return newFlags.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        public string FragmentationOffset
        {
            get
            {
                var newOffset = _flags << 3;
                newOffset >>= 3;
                return newOffset.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string TTL
        {
            get { return _TTL.ToString(CultureInfo.InvariantCulture); }
        }

        public Protocol ProtocolType
        {
            get
            {
                switch (_protocol)
                {
                    case 6: return Protocol.TCP;
                    case 17: return Protocol.UDP;
                    default: return Protocol.Unknown;
                }
            }
        }

        public string Checksum
        {
            get { return $"0x{_checksum:x2}"; }
        }

        public IPAddress SourceAddress
        {
            get { return new IPAddress(_sourceIPAddress); }
        }

        public IPAddress DestinationAddress
        {
            get { return new IPAddress(_destinationIPAddress); }
        }

        public string TotalLength
        {
            get { return _totalLength.ToString(CultureInfo.InvariantCulture); }
        }

        public string Identification
        {
            get { return _identification.ToString(CultureInfo.InvariantCulture); }
        }

        public List<byte> Data
        {
            get { return _data; }
        }
    }
}

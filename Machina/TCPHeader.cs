// Machina ~ TCPHeader.cs
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
    public class TCPHeader
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private readonly uint _acknowledgementNumber = 555;
        private readonly short _checksum = 555;
        private readonly List<byte> _data = new List<byte>();
        private readonly ushort _destinationPort;
        private readonly ushort _flags = 555;
        private readonly byte _headerLength;
        private readonly ushort _messageLength;
        private readonly uint _sequenceNumber = 555;
        private readonly ushort _sourcePort;
        private readonly ushort _urgentPointer;
        private readonly ushort _window = 555;

        public TCPHeader(byte[] byBuffer, int nReceived)
        {
            try
            {
                using (var memoryStream = new MemoryStream(byBuffer, 0, nReceived))
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        _sourcePort = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _destinationPort = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _sequenceNumber = (uint) IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                        _acknowledgementNumber = (uint) IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
                        _flags = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _window = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _checksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _urgentPointer = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _headerLength = (byte) (_flags >> 12);
                        _headerLength *= 4;
                        _messageLength = (ushort) (nReceived - _headerLength);
                        if (_messageLength > 0)
                        {
                            var tempData = new byte[_messageLength];
                            Array.Copy(byBuffer, _headerLength, tempData, 0, _messageLength);
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

        public string SourcePort
        {
            get { return _sourcePort.ToString(CultureInfo.InvariantCulture); }
        }

        public string DestinationPort
        {
            get { return _destinationPort.ToString(CultureInfo.InvariantCulture); }
        }

        public string SequenceNumber
        {
            get { return _sequenceNumber.ToString(CultureInfo.InvariantCulture); }
        }

        public string AcknowledgementNumber
        {
            get { return (_flags & 0x10) != 0 ? _acknowledgementNumber.ToString(CultureInfo.InvariantCulture) : string.Empty; }
        }

        public string HeaderLength
        {
            get { return _headerLength.ToString(CultureInfo.InvariantCulture); }
        }

        public string WindowSize
        {
            get { return _window.ToString(CultureInfo.InvariantCulture); }
        }

        public string UrgentPointer
        {
            get { return (_flags & 0x20) != 0 ? _urgentPointer.ToString(CultureInfo.InvariantCulture) : string.Empty; }
        }

        public string Flags
        {
            get
            {
                var newFlags = _flags & 0x3F;
                var stringFlags = $"0x{newFlags:x2} (";
                if ((newFlags & 0x01) != 0)
                {
                    stringFlags += "FIN, ";
                }
                if ((newFlags & 0x02) != 0)
                {
                    stringFlags += "SYN, ";
                }
                if ((newFlags & 0x04) != 0)
                {
                    stringFlags += "RST, ";
                }
                if ((newFlags & 0x08) != 0)
                {
                    stringFlags += "PSH, ";
                }
                if ((newFlags & 0x10) != 0)
                {
                    stringFlags += "ACK, ";
                }
                if ((newFlags & 0x20) != 0)
                {
                    stringFlags += "URG";
                }
                stringFlags += ")";
                if (stringFlags.Contains("()"))
                {
                    stringFlags = stringFlags.Remove(stringFlags.Length - 3);
                }
                else if (stringFlags.Contains(", )"))
                {
                    stringFlags = stringFlags.Remove(stringFlags.Length - 3, 2);
                }
                return stringFlags;
            }
        }

        public string Checksum
        {
            get { return $"0x{_checksum:x2}"; }
        }

        public List<byte> Data
        {
            get { return _data; }
        }

        public ushort MessageLength
        {
            get { return _messageLength; }
        }
    }
}

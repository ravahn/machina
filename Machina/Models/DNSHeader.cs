// Machina ~ DNSHeader.cs
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
using System.Globalization;
using System.IO;
using System.Net;
using NLog;

namespace Machina.Models
{
    public class DNSHeader
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        private readonly ushort _flags;
        private readonly ushort _identification;
        private readonly ushort _totalAdditionalRR;
        private readonly ushort _totalAnswerRR;
        private readonly ushort _totalAuthorityRR;
        private readonly ushort _totalQuestions;

        public DNSHeader(byte[] byBuffer, int nReceived)
        {
            try
            {
                using (var memoryStream = new MemoryStream(byBuffer, 0, nReceived))
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        _identification = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _flags = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _totalQuestions = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _totalAnswerRR = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _totalAuthorityRR = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                        _totalAdditionalRR = (ushort) IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
                    }
                }
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, ex);
            }
        }

        public string Identification
        {
            get { return $"0x{_identification:x2}"; }
        }

        public string Flags
        {
            get { return $"0x{_flags:x2}"; }
        }

        public string TotalQuestions
        {
            get { return _totalQuestions.ToString(CultureInfo.InvariantCulture); }
        }

        public string TotalAnswerRR
        {
            get { return _totalAnswerRR.ToString(CultureInfo.InvariantCulture); }
        }

        public string TotalAuthorityRR
        {
            get { return _totalAuthorityRR.ToString(CultureInfo.InvariantCulture); }
        }

        public string TotalAdditionalRR
        {
            get { return _totalAdditionalRR.ToString(CultureInfo.InvariantCulture); }
        }
    }
}

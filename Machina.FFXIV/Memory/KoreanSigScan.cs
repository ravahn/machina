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

namespace Machina.FFXIV.Memory
{
    public class KoreanSigScan : SigScan
    {
        protected override Dictionary<SignatureType, int[]> Signatures => new Dictionary<SignatureType, int[]>()
        {
            { SignatureType.OodleNetwork1_Shared_Size, SignatureStringToByteArray("48 83 7b ** 00 75 ** b9 13 00 00 00 e8") },
            { SignatureType.OodleNetwork1_Shared_SetWindow, SignatureStringToByteArray("44 8b 4b ** 48 8b c8 4c 8b 43 ** 8b 53 ** 48 89 43 ** e8") },
            { SignatureType.OodleNetwork1UDP_Train, SignatureStringToByteArray("48 8b 53 ?? 45 33 c9 48 8b 4b ?? 45 33 c0 c7 44 24 20 00 00 00 00 e8") },
            { SignatureType.OodleNetwork1UDP_Decode, SignatureStringToByteArray("44 8b 4c 24 ?? 4c 8b c6 48 8b 55 ?? 48 8b 4d ?? 48 89 ?? 24 28 48 89 ?? 24 20 e8") },
            { SignatureType.OodleNetwork1UDP_State_Size, SignatureStringToByteArray("44 8b 4b ** 48 8b c8 4c 8b 43 ** 8b 53 ** 48 89 43 ** e8 ** ** ** ** 48 83 7b ** 00 ** ** e8") },
            { SignatureType.OodleNetwork1UDP_Encode, SignatureStringToByteArray("48 8b 57 ?? 4c 8b ce 48 8b 4f ?? 4c 8b c5 4c 89 ?? 24 20 e8" ) },
            { SignatureType.OodleMalloc, SignatureStringToByteArray("41 be 00 00 00 40 ba 10 00 00 00 49 8b ce ff 15" ) },
            { SignatureType.OodleFree, SignatureStringToByteArray("48 8b cb f3 ab 4d 85 c0 74 ?? 49 8b c8 ff 15" ) },

            { SignatureType.OodleNetwork1TCP_State_Size, null },
            { SignatureType.OodleNetwork1TCP_Train, null },
            { SignatureType.OodleNetwork1TCP_Decode, null },
            { SignatureType.OodleNetwork1TCP_Encode, null },
        };
    }
}

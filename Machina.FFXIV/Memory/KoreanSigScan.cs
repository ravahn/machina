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
            { SignatureType.OodleNetwork1_Shared_Size, SignatureStringToByteArray("48 83 7b ** 00 75 ** b9 11 00 00 00 e8") },
            { SignatureType.OodleNetwork1_Shared_SetWindow, SignatureStringToByteArray("4c 8b 43 ** 41 b9 00 00 10 00 ba ** 00 00 00 48 89 43 ** 48 8b c8 e8") },
            { SignatureType.OodleNetwork1UDP_Train, null },
            { SignatureType.OodleNetwork1UDP_Decode, SignatureStringToByteArray("74 ** 49 8b ca e8 ** ** ** ** eb ** 48 8b 49 ** e8") },
            { SignatureType.OodleNetwork1UDP_State_Size, SignatureStringToByteArray("48 8b 44 24 ** 48 8b 78 ** 4d 85 ed 75 ** 48 89 7e ** e8") },
            { SignatureType.OodleNetwork1UDP_Encode, SignatureStringToByteArray("48 83 c7 02 4d 8b c4 48 89 7c ** ** e8" ) },
            { SignatureType.OodleMalloc, SignatureStringToByteArray("41 be 00 00 00 40 ba 10 00 00 00 49 8b ce ff 15" ) },
            { SignatureType.OodleFree, SignatureStringToByteArray("48 8b cb f3 ab 4d 85 c0 74 ?? 49 8b c8 ff 15" ) },
            { SignatureType.OodleNetwork1TCP_State_Size, SignatureStringToByteArray("4d 85 ed 75 ** 48 89 7e ** e8 ** ** ** ** 4c 8b f0 e8") },
            { SignatureType.OodleNetwork1TCP_Train, SignatureStringToByteArray("89 5c ** ** 83 fd 01 75 ** 48 8b 0f e8") },
            { SignatureType.OodleNetwork1TCP_Decode, SignatureStringToByteArray("4c 8b 11 48 89 6c ** ** 4d 85 d2 74 ** 49 8b ca e8 ") },
            { SignatureType.OodleNetwork1TCP_Encode, SignatureStringToByteArray("48 8b ** 48 8d ** ** ** c6 44 ** ** ** 49 8b ** 48 89 44 ** ** e8" ) },
        };
    }
}

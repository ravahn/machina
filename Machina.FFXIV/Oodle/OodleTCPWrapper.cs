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

namespace Machina.FFXIV.Oodle
{
    public class OodleTCPWrapper : IOodleWrapper
    {
        private const byte HashtableBits = 17; // 19 for UDP
        private const int WindowSize = 0x100000; // 0x16000

        private readonly byte[] _state;
        private readonly byte[] _shared;
        private readonly byte[] _window = new byte[WindowSize];

        private readonly IOodleNative _oodleNative;

        public OodleTCPWrapper(IOodleNative native)
        {
            _oodleNative = native;

            int stateSize = _oodleNative.OodleNetwork1TCP_State_Size(); // change for UDP
            int sharedSize = _oodleNative.OodleNetwork1_Shared_Size(HashtableBits);

            _state = new byte[stateSize];
            _shared = new byte[sharedSize];

            _oodleNative.OodleNetwork1_Shared_SetWindow(_shared, HashtableBits, _window, _window.Length);

            _oodleNative.OodleNetwork1TCP_Train(_state, _shared, IntPtr.Zero, IntPtr.Zero, 0); // change for UDP
        }

        public unsafe bool Decompress(byte[] payload, int offset, int compressedLength, byte[] plaintext, int decompressedLength)
        {
            fixed (byte* pPayload = payload)
            {
                if (!_oodleNative.OodleNetwork1TCP_Decode(_state, _shared, new IntPtr(pPayload + offset), // change for UDP
                    compressedLength, plaintext, decompressedLength))
                    return false;
            }

            return true;
        }
    }
}

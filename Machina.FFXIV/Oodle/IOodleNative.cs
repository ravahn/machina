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
    public interface IOodleNative
    {
        void Initialize(string path);
        void UnInitialize();

        int OodleNetwork1UDP_State_Size();
        int OodleNetwork1_Shared_Size(int htbits);
        void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] window, int windowSize);
        void OodleNetwork1UDP_Train(byte[] state, byte[] share, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets);
        unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] share, IntPtr compressed, int compressedSize, byte[] raw, int rawSize);
        bool OodleNetwork1UDP_Encode(byte[] state, byte[] share, byte[] raw, int rawSize, byte[] compressed);
    }
}

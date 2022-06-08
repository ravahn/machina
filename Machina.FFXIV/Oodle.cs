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

using System.Runtime.InteropServices;

namespace Machina.FFXIV
{
    public class Oodle
    {
        private static class OodleNative
        {
            [DllImport("oo2net_9_win64")]
            public static extern int OodleNetwork1_Shared_Size(int htbits);

            [DllImport("oo2net_9_win64")]
            public static extern unsafe void OodleNetwork1UDP_Train(byte[] state, byte[] shared,
                void** training_packet_pointers, int* training_packet_sizes, int num_training_packets);

            [DllImport("oo2net_9_win64")]
            public static extern unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] shared, byte* comp, int compLen,
                byte[] raw, int rawLen);

            [DllImport("oo2net_9_win64")]
            public static extern void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] windowv, int window_size);

            [DllImport("oo2net_9_win64")]
            public static extern int OodleNetwork1UDP_State_Size();


        }

        private const byte HashtableBits = 19;
        private const int WindowSize = 0x8000;


        private readonly byte[] State;
        private readonly byte[] Shared;
        private readonly byte[] Window;

        public Oodle()
        {
            int stateSize = OodleNative.OodleNetwork1UDP_State_Size();
            int sharedSize = OodleNative.OodleNetwork1_Shared_Size(HashtableBits);

            State = new byte[stateSize];
            Shared = new byte[sharedSize];
            Window = new byte[WindowSize];

            Initialize();
        }

        private void Initialize()
        {
            OodleNative.OodleNetwork1_Shared_SetWindow(Shared, HashtableBits, Window, Window.Length);

            unsafe
            {
                OodleNative.OodleNetwork1UDP_Train(State, Shared, null, null, 0);
            }
        }

        public unsafe bool Decompress(byte[] payload, int offset, int compressedLength, byte[] plaintext, int decompressedLength)
        {
            OodleNative.OodleNetwork1_Shared_SetWindow(Shared, HashtableBits, Window, Window.Length);
            fixed (byte* pPayload = payload)
            {
                if (!OodleNative.OodleNetwork1UDP_Decode(State, Shared, pPayload + offset, compressedLength, plaintext,
                            decompressedLength))
                    return false;
            }

            return true;

        }
    }
}


using System;
using System.Runtime.InteropServices;

namespace Machina.FFXIV.Oodle
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
            public static extern unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] shared, byte *comp, int compLen,
                byte[] raw, int rawLen);

            [DllImport("oo2net_9_win64")]
            public static extern void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] windowv, int window_size);

            [DllImport("oo2net_9_win64")]
            public static extern int OodleNetwork1UDP_State_Size();


        }

        private const byte HashtableBits = 19;
        private const int WindowSize = 0x8000;


        private byte[] State;
        private byte[] Shared;
        private byte[] Window;

        public Oodle()
        {
            var stateSize = OodleNative.OodleNetwork1UDP_State_Size();
            var sharedSize = OodleNative.OodleNetwork1_Shared_Size(HashtableBits);
            
            State = new byte[stateSize];
            Shared = new byte[sharedSize];
            Window = new byte[WindowSize];
        }
        
        private void Initialize()
        {
            OodleNative.OodleNetwork1_Shared_SetWindow(Shared, HashtableBits, Window, Window.Length);

            unsafe
            {
                OodleNative.OodleNetwork1UDP_Train(State, Shared, null, null, 0);
            }
        }
        
        public unsafe void Decompress(byte[] payload, int offset, int compressedLength, byte[] plaintext, int decompressedLength)
        {
            Initialize();

            fixed (byte* pPayload = payload)
            {
                if (!OodleNative.OodleNetwork1UDP_Decode(State, Shared, pPayload + offset, compressedLength, plaintext,
                            decompressedLength))
                    throw new Exception("Oodle decompression failed");
            }
            
        }
    }
}

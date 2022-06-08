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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Machina.FFXIV
{

    internal static class FFXIVOodle_Native
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        internal delegate int OodleNetwork1UDP_State_Size1();
        internal delegate int OodleNetwork1_Shared_Size(int htbits);
        internal delegate void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] window, int windowSize);
        internal delegate void OodleNetwork1UDP_Train(byte[] state, byte[] shared, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets);
        internal unsafe delegate bool OodleNetwork1UDP_Decode(byte[] state, byte[] shared, byte* compressed, int compressedSize, byte[] raw, int rawSize);
        internal delegate bool OodleNetwork1UDP_Encode(byte[] state, byte[] shared, byte[] raw, int rawSize, byte[] compressed);
        internal delegate IntPtr OodleMalloc(IntPtr a, int b);
        internal delegate void OodleFree(IntPtr a);

        private static readonly int off_OodleMalloc = 0x1f21cf8;
        private static readonly int off_OodleFree = 0x1f21d00;
        private static readonly int off_OodleNetwork1_Shared_Size = 0x153edf0;
        private static readonly int off_OodleNetwork1_Shared_SetWindow = 0x153ecc0;
        private static readonly int off_OodleNetwork1UDP_Train = 0x153d920;
        private static readonly int off_OodleNetwork1UDP_Decode = 0x153cdd0;
        private static readonly int off_OodleNetwork1UDP_Encode = 0x153ce20;
        private static readonly int off_OodleNetwork1UDP_State_Size = 0x153d470;


        internal static OodleNetwork1UDP_State_Size1 fnptrOodleNetwork1UDP_State_Size;

        internal static OodleNetwork1_Shared_Size fnptrOodleNetwork1_Shared_Size;

        internal static OodleNetwork1_Shared_SetWindow fnptrOodleNetwork1_Shared_SetWindow;

        internal static OodleNetwork1UDP_Train fnptrOodleNetwork1UDP_Train;

        internal static OodleNetwork1UDP_Encode fnptrOodleNetwork1UDP_Encode;

        internal static OodleNetwork1UDP_Decode fnptrOodleNetwork1UDP_Decode;

        internal static OodleMalloc fnptrOodleMalloc;
        internal static OodleFree fnptrOodleFree;


        private static unsafe IntPtr AllocAlignedMemory(IntPtr cb, int alignment)
        {
            // copied from https://github.com/dotnet/runtime/issues/33244#issuecomment-595848832
            if (alignment % 1 != 0)
                throw new ArgumentException();

            IntPtr block = Marshal.AllocHGlobal(checked(cb + sizeof(IntPtr) + (alignment - 1)));

            // Align the pointer
            IntPtr aligned = (IntPtr)((long)(block + sizeof(IntPtr) + (alignment - 1)) & ~(alignment - 1));

            // Store the pointer to the memory block to free right before the aligned pointer 
            *(((IntPtr*)aligned) - 1) = block;

            return aligned;
        }

        private static unsafe void FreeAlignedMemory(IntPtr p)
        {
            if (p != IntPtr.Zero)
                Marshal.FreeHGlobal(*(((IntPtr*)p) - 1));
        }

        private static IntPtr _ffxivLibraryHandle = IntPtr.Zero;

        internal static void Initialize(string gamePath)
        {
            if (_ffxivLibraryHandle != IntPtr.Zero)
                return;

            _ffxivLibraryHandle = LoadLibrary(gamePath);
            if (_ffxivLibraryHandle == IntPtr.Zero)
            {
                Trace.WriteLine($"FFXIVOodle_Native: Cannot load ffxiv_dx11 executable at path {gamePath}.", "DEBUG-MACHINA");
                return;
            }
            else
                Trace.WriteLine($"FFXIVOodle_Native: Loaded ffxiv_dx11 executable into ACT memory from path {gamePath}.", "DEBUG-MACHINA");

            fnptrOodleMalloc = new OodleMalloc(AllocAlignedMemory);
            fnptrOodleFree = new OodleFree(FreeAlignedMemory);

            IntPtr myMallocPtr = Marshal.GetFunctionPointerForDelegate(fnptrOodleMalloc);
            IntPtr myFreePtr = Marshal.GetFunctionPointerForDelegate(fnptrOodleFree);

            Marshal.Copy(BitConverter.GetBytes(myMallocPtr.ToInt64()), 0, _ffxivLibraryHandle + off_OodleMalloc, IntPtr.Size);
            Marshal.Copy(BitConverter.GetBytes(myFreePtr.ToInt64()), 0, _ffxivLibraryHandle + off_OodleFree, IntPtr.Size);

            fnptrOodleNetwork1UDP_State_Size = (OodleNetwork1UDP_State_Size1)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1UDP_State_Size, typeof(OodleNetwork1UDP_State_Size1));

            fnptrOodleNetwork1_Shared_Size = (OodleNetwork1_Shared_Size)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1_Shared_Size, typeof(OodleNetwork1_Shared_Size));

            fnptrOodleNetwork1_Shared_SetWindow = (OodleNetwork1_Shared_SetWindow)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1_Shared_SetWindow, typeof(OodleNetwork1_Shared_SetWindow));

            fnptrOodleNetwork1UDP_Train = (OodleNetwork1UDP_Train)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1UDP_Train, typeof(OodleNetwork1UDP_Train));

            fnptrOodleNetwork1UDP_Encode = (OodleNetwork1UDP_Encode)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1UDP_Encode, typeof(OodleNetwork1UDP_Encode));

            fnptrOodleNetwork1UDP_Decode = (OodleNetwork1UDP_Decode)Marshal.GetDelegateForFunctionPointer(
                _ffxivLibraryHandle + off_OodleNetwork1UDP_Decode, typeof(OodleNetwork1UDP_Decode));
        }

        internal static void UnInitialize()
        {
            if (_ffxivLibraryHandle != IntPtr.Zero)
                _ = FreeLibrary(_ffxivLibraryHandle);
            _ffxivLibraryHandle = IntPtr.Zero;
            fnptrOodleNetwork1UDP_Decode = null;
        }

    }

    internal class FFXIVOodle
    {
        private byte[] _state;
        private byte[] _shared;
        private readonly byte[] _window = new byte[0x16000];
        public unsafe void Initialize()
        {
            int htbits = 0x13;
            _state = new byte[FFXIVOodle_Native.fnptrOodleNetwork1UDP_State_Size()];
            _shared = new byte[FFXIVOodle_Native.fnptrOodleNetwork1_Shared_Size(htbits)];

            FFXIVOodle_Native.fnptrOodleNetwork1_Shared_SetWindow(_shared, htbits, _window, _window.Length);

            FFXIVOodle_Native.fnptrOodleNetwork1UDP_Train(_state, _shared, IntPtr.Zero, IntPtr.Zero, 0);
        }

        public unsafe bool Decompress(byte[] payload, int offset, int compressedLength, byte[] plaintext, int decompressedLength)
        {
            if (FFXIVOodle_Native.fnptrOodleNetwork1UDP_Decode == null)
                return false;

            fixed (byte* pPayload = payload)
            {
                if (!FFXIVOodle_Native.fnptrOodleNetwork1UDP_Decode(_state, _shared, pPayload + offset,
                    compressedLength, plaintext, decompressedLength))
                    return false;
            }

            return true;
        }
    }
}

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

namespace Machina.FFXIV.Oodle
{
    public class OodleNative_Library : IOodleNative
    {
        private delegate int OodleNetwork1UDP_State_Size_Func();
        private delegate int OodleNetwork1TCP_State_Size_Func();
        private delegate int OodleNetwork1_Shared_Size_Func(int htbits);
        private delegate void OodleNetwork1_Shared_SetWindow_Action(byte[] data, int htbits, byte[] window, int windowSize);
        private delegate void OodleNetwork1UDP_Train_Action(byte[] state, byte[] shared, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets);
        private delegate void OodleNetwork1TCP_Train_Action(byte[] state, byte[] shared, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets);
        private unsafe delegate bool OodleNetwork1UDP_Decode_Func(byte[] state, byte[] shared, byte* compressed, int compressedSize, byte[] raw, int rawSize);
        private unsafe delegate bool OodleNetwork1TCP_Decode_Func(byte[] state, byte[] shared, byte* compressed, int compressedSize, byte[] raw, int rawSize);
        private delegate bool OodleNetwork1UDP_Encode_Func(byte[] state, byte[] shared, byte[] raw, int rawSize, byte[] compressed);
        private delegate bool OodleNetwork1TCP_Encode_Func(byte[] state, byte[] shared, byte[] raw, int rawSize, byte[] compressed);

        private OodleNetwork1UDP_State_Size_Func _OodleNetwork1UDP_State_Size;
        private OodleNetwork1TCP_State_Size_Func _OodleNetwork1TCP_State_Size;
        private OodleNetwork1_Shared_Size_Func _OodleNetwork1_Shared_Size;
        private OodleNetwork1_Shared_SetWindow_Action _OodleNetwork1_Shared_SetWindow;
        private OodleNetwork1UDP_Train_Action _OodleNetwork1UDP_Train;
        private OodleNetwork1TCP_Train_Action _OodleNetwork1TCP_Train;
        private OodleNetwork1UDP_Decode_Func _OodleNetwork1UDP_Decode;
        private OodleNetwork1TCP_Decode_Func _OodleNetwork1TCP_Decode;
        private OodleNetwork1UDP_Encode_Func _OodleNetwork1UDP_Encode;
        private OodleNetwork1TCP_Encode_Func _OodleNetwork1TCP_Encode;

        private IntPtr _libraryHandle = IntPtr.Zero;
        private readonly object _librarylock = new object();

        public bool Initialized { get; set; }

        public void Initialize(string path)
        {
            try
            {
                lock (_librarylock)
                {
                    if (_libraryHandle != IntPtr.Zero)
                        return;

                    _libraryHandle = NativeMethods.LoadLibraryW(path);
                    if (_libraryHandle == IntPtr.Zero)
                    {
                        Trace.WriteLine($"{nameof(OodleNative_Library)}: Cannot load oodle library at path {path}.", "DEBUG-MACHINA");
                        return;
                    }
                    else
                        Trace.WriteLine($"{nameof(OodleNative_Library)}: Loaded oodle library from path {path}.", "DEBUG-MACHINA");

                    IntPtr address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1UDP_State_Size));
                    _OodleNetwork1UDP_State_Size = (OodleNetwork1UDP_State_Size_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1UDP_State_Size_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1TCP_State_Size));
                    _OodleNetwork1TCP_State_Size = (OodleNetwork1TCP_State_Size_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1TCP_State_Size_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1_Shared_Size));
                    _OodleNetwork1_Shared_Size = (OodleNetwork1_Shared_Size_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1_Shared_Size_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1_Shared_SetWindow));
                    _OodleNetwork1_Shared_SetWindow = (OodleNetwork1_Shared_SetWindow_Action)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1_Shared_SetWindow_Action));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1UDP_Train));
                    _OodleNetwork1UDP_Train = (OodleNetwork1UDP_Train_Action)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1UDP_Train_Action));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1TCP_Train));
                    _OodleNetwork1TCP_Train = (OodleNetwork1TCP_Train_Action)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1TCP_Train_Action));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1UDP_Decode));
                    _OodleNetwork1UDP_Decode = (OodleNetwork1UDP_Decode_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1UDP_Decode_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1TCP_Decode));
                    _OodleNetwork1TCP_Decode = (OodleNetwork1TCP_Decode_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1TCP_Decode_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1UDP_Encode));
                    _OodleNetwork1UDP_Encode = (OodleNetwork1UDP_Encode_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1UDP_Encode_Func));

                    address = NativeMethods.GetProcAddress(_libraryHandle, nameof(OodleNetwork1UDP_Encode));
                    _OodleNetwork1TCP_Encode = (OodleNetwork1TCP_Encode_Func)Marshal.GetDelegateForFunctionPointer(
                        address, typeof(OodleNetwork1TCP_Encode_Func));

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(OodleNative_Library)}: Exception in {nameof(Initialize)}. Library path: {path}  Exception: {ex}", "DEBUG-MACHINA");

                UnInitialize();
            }
        }

        public void UnInitialize()
        {
            try
            {
                lock (_librarylock)
                {
                    Initialized = false;

                    if (_libraryHandle != IntPtr.Zero)
                    {
                        bool freed = NativeMethods.FreeLibrary(_libraryHandle);
                        if (!freed)
                            Trace.WriteLine($"{nameof(OodleNative_Library)}: {nameof(NativeMethods.FreeLibrary)} failed.", "DEBUG-MACHINA");
                        _libraryHandle = IntPtr.Zero;
                    }

                    _OodleNetwork1UDP_State_Size = null;
                    _OodleNetwork1TCP_State_Size = null;
                    _OodleNetwork1_Shared_Size = null;
                    _OodleNetwork1_Shared_SetWindow = null;
                    _OodleNetwork1UDP_Train = null;
                    _OodleNetwork1TCP_Train = null;
                    _OodleNetwork1UDP_Decode = null;
                    _OodleNetwork1TCP_Decode = null;
                    _OodleNetwork1UDP_Encode = null;
                    _OodleNetwork1TCP_Encode = null;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(OodleNative_Library)}: exception in {nameof(UnInitialize)}: {ex}", "DEBUG-MACHINA");
            }
        }

        public int OodleNetwork1UDP_State_Size()
        {
            if (!Initialized)
                return 0;
            return _OodleNetwork1UDP_State_Size.Invoke();
        }

        public int OodleNetwork1TCP_State_Size()
        {
            if (!Initialized)
                return 0;
            return _OodleNetwork1TCP_State_Size.Invoke();
        }

        public int OodleNetwork1_Shared_Size(int htbits)
        {
            if (!Initialized)
                return 0;

            return _OodleNetwork1_Shared_Size.Invoke(htbits);
        }

        public void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] window, int windowSize)
        {
            if (Initialized)
                _OodleNetwork1_Shared_SetWindow.Invoke(data, htbits, window, windowSize);
        }

        public void OodleNetwork1UDP_Train(byte[] state, byte[] share, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets)
        {
            if (Initialized)
                _OodleNetwork1UDP_Train.Invoke(state, share, training_packet_pointers, training_packet_sizes, num_training_packets);
        }

        public void OodleNetwork1TCP_Train(byte[] state, byte[] share, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets)
        {
            if (Initialized)
                _OodleNetwork1TCP_Train.Invoke(state, share, training_packet_pointers, training_packet_sizes, num_training_packets);
        }

        public unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] share, IntPtr compressed, int compressedSize, byte[] raw, int rawSize)
        {
            if (!Initialized)
                return false;
            return _OodleNetwork1UDP_Decode.Invoke(state, share, (byte*)compressed, compressedSize, raw, rawSize);
        }

        public unsafe bool OodleNetwork1TCP_Decode(byte[] state, byte[] share, IntPtr compressed, int compressedSize, byte[] raw, int rawSize)
        {
            if (!Initialized)
                return false;
            return _OodleNetwork1TCP_Decode.Invoke(state, share, (byte*)compressed, compressedSize, raw, rawSize);
        }

        public bool OodleNetwork1UDP_Encode(byte[] state, byte[] share, byte[] raw, int rawSize, byte[] compressed)
        {
            if (!Initialized)
                return false;

            return _OodleNetwork1UDP_Encode.Invoke(state, share, raw, rawSize, compressed);
        }
        public bool OodleNetwork1TCP_Encode(byte[] state, byte[] share, byte[] raw, int rawSize, byte[] compressed)
        {
            if (!Initialized)
                return false;

            return _OodleNetwork1TCP_Encode.Invoke(state, share, raw, rawSize, compressed);
        }
    }
}

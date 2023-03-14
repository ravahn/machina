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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Machina.FFXIV.Memory;

namespace Machina.FFXIV.Oodle
{
    public class OodleNative_Ffxiv : IOodleNative
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
        private delegate IntPtr OodleMalloc_Func(IntPtr a, int b);
        private delegate void OodleFree_Action(IntPtr a);


        private static Dictionary<SignatureType, int> _offsets;

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
        private OodleMalloc_Func _OodleMalloc;
        private OodleFree_Action _OodleFree;

        private readonly ISigScan _sigscan;

        public OodleNative_Ffxiv(ISigScan sigscan)
        {
            _sigscan = sigscan;
        }

        private static unsafe IntPtr AllocAlignedMemory(IntPtr cb, int alignment)
        {
            // copied from https://github.com/dotnet/runtime/issues/33244#issuecomment-595848832
            if (alignment % 1 != 0)
                throw new ArgumentException($"{nameof(AllocAlignedMemory)}: {nameof(alignment)} % 1 != 0)");

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

        private IntPtr _libraryHandle = IntPtr.Zero;
        private readonly object _librarylock = new object();
        private string _libraryTempPath = string.Empty;

        public bool Initialized { get; set; }
        public void Initialize(string path)
        {

            try
            {
                if (!File.Exists(path))
                {
                    Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: ffxiv_dx11 executable at path {path} does not exist.", "DEBUG-MACHINA");
                    return;
                }

                lock (_librarylock)
                {
                    if (_libraryHandle != IntPtr.Zero)
                        return;

                    // Copy file to temp directory
                    _libraryTempPath = Path.Combine(Path.GetTempPath(), "Machina.FFXIV", Guid.NewGuid().ToString() + ".exe");

                    if (!Directory.Exists(_libraryTempPath.Substring(0, _libraryTempPath.LastIndexOf("\\", StringComparison.Ordinal) + 1)))
                        _ = Directory.CreateDirectory(_libraryTempPath.Substring(0, _libraryTempPath.LastIndexOf("\\", StringComparison.Ordinal) + 1));

                    File.Copy(path, _libraryTempPath, true);

                    _libraryHandle = NativeMethods.LoadLibraryW(_libraryTempPath);
                    if (_libraryHandle == IntPtr.Zero)
                    {
                        Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Cannot load ffxiv_dx11 executable at path {path}.", "DEBUG-MACHINA");
                        return;
                    }
                    else
                        Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Copied and loaded ffxiv_dx11 executable into ACT memory from path {path}.", "DEBUG-MACHINA");

                    _offsets = _sigscan.Read(_libraryHandle);

                    _OodleMalloc = new OodleMalloc_Func(AllocAlignedMemory);
                    _OodleFree = new OodleFree_Action(FreeAlignedMemory);

                    IntPtr myMallocPtr = Marshal.GetFunctionPointerForDelegate(_OodleMalloc);
                    IntPtr myFreePtr = Marshal.GetFunctionPointerForDelegate(_OodleFree);

                    Marshal.Copy(BitConverter.GetBytes(myMallocPtr.ToInt64()), 0, IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleMalloc]), IntPtr.Size);
                    Marshal.Copy(BitConverter.GetBytes(myFreePtr.ToInt64()), 0, IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleFree]), IntPtr.Size);

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1UDP_State_Size))
                        _OodleNetwork1UDP_State_Size = (OodleNetwork1UDP_State_Size_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1UDP_State_Size]), typeof(OodleNetwork1UDP_State_Size_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1TCP_State_Size))
                        _OodleNetwork1TCP_State_Size = (OodleNetwork1TCP_State_Size_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1TCP_State_Size]), typeof(OodleNetwork1TCP_State_Size_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1_Shared_Size))
                        _OodleNetwork1_Shared_Size = (OodleNetwork1_Shared_Size_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1_Shared_Size]), typeof(OodleNetwork1_Shared_Size_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1_Shared_SetWindow))
                        _OodleNetwork1_Shared_SetWindow = (OodleNetwork1_Shared_SetWindow_Action)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1_Shared_SetWindow]), typeof(OodleNetwork1_Shared_SetWindow_Action));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1UDP_Train))
                        _OodleNetwork1UDP_Train = (OodleNetwork1UDP_Train_Action)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1UDP_Train]), typeof(OodleNetwork1UDP_Train_Action));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1TCP_Train))
                        _OodleNetwork1TCP_Train = (OodleNetwork1TCP_Train_Action)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1TCP_Train]), typeof(OodleNetwork1TCP_Train_Action));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1UDP_Decode))
                        _OodleNetwork1UDP_Decode = (OodleNetwork1UDP_Decode_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1UDP_Decode]), typeof(OodleNetwork1UDP_Decode_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1TCP_Decode))
                        _OodleNetwork1TCP_Decode = (OodleNetwork1TCP_Decode_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1TCP_Decode]), typeof(OodleNetwork1TCP_Decode_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1UDP_Encode))
                        _OodleNetwork1UDP_Encode = (OodleNetwork1UDP_Encode_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1UDP_Encode]), typeof(OodleNetwork1UDP_Encode_Func));

                    if (_offsets.ContainsKey(SignatureType.OodleNetwork1TCP_Encode))
                        _OodleNetwork1TCP_Encode = (OodleNetwork1TCP_Encode_Func)Marshal.GetDelegateForFunctionPointer(
                            IntPtr.Add(_libraryHandle, _offsets[SignatureType.OodleNetwork1TCP_Encode]), typeof(OodleNetwork1TCP_Encode_Func));

                    if (_OodleNetwork1UDP_State_Size == null || _OodleNetwork1_Shared_Size == null || _OodleNetwork1_Shared_SetWindow == null ||
                        _OodleNetwork1UDP_Train == null || _OodleNetwork1UDP_Decode == null || _OodleNetwork1UDP_Encode == null)
                    {
                        Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: ERROR: Cannot find one or more signatures in ffxiv_dx11 executable.  Unable to decompress packet data.", "DEBUG-MACHINA");

                        UnInitialize();
                        return;
                    }
                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Exception in {nameof(Initialize)}. Game path: {path}  Exception: {ex}", "DEBUG-MACHINA");

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
                            Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: {nameof(NativeMethods.FreeLibrary)} failed.", "DEBUG-MACHINA");
                        _libraryHandle = IntPtr.Zero;
                    }

                    if (File.Exists(_libraryTempPath))
                    {
                        try
                        {
                            File.Delete(_libraryTempPath);
                        }
                        catch
                        {
                            Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: {nameof(NativeMethods.FreeLibrary)} could not delete temp file.", "DEBUG-MACHINA");
                        }
                    }
                    _libraryTempPath = string.Empty;

                    _OodleMalloc = null;
                    _OodleFree = null;
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
                Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: exception in {nameof(UnInitialize)}: {ex}", "DEBUG-MACHINA");
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

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
using System.Linq;
using System.Runtime.InteropServices;

namespace Machina.FFXIV.Memory
{
    public enum SignatureType
    {
        OodleNetwork1_Shared_Size = 1,
        OodleNetwork1_Shared_SetWindow = 2,
        OodleNetwork1UDP_Train = 3,
        OodleNetwork1UDP_Decode = 4,
        OodleNetwork1UDP_State_Size = 5,
        OodleNetwork1UDP_Encode = 6,
        OodleMalloc = 7,
        OodleFree = 8
    }

    public class SigScan
    {
        private readonly Dictionary<SignatureType, int[]> _signatures = new Dictionary<SignatureType, int[]>()
        {
            { SignatureType.OodleNetwork1_Shared_Size, SignatureStringToByteArray("48 83 7b ** 00 75 ** b9 13 00 00 00 e8") },
            { SignatureType.OodleNetwork1_Shared_SetWindow, SignatureStringToByteArray("44 8b 4b ** 48 8b c8 4c 8b 43 ** 8b 53 ** 48 89 43 ** e8") },
            { SignatureType.OodleNetwork1UDP_Train, SignatureStringToByteArray("48 8b 53 ?? 45 33 c9 48 8b 4b ?? 45 33 c0 c7 44 24 20 00 00 00 00 e8") },
            { SignatureType.OodleNetwork1UDP_Decode, SignatureStringToByteArray("44 8b 4c 24 ?? 4c 8b c6 48 8b 55 ?? 48 8b 4d ?? 48 89 ?? 24 28 48 89 ?? 24 20 e8") },
            { SignatureType.OodleNetwork1UDP_State_Size, SignatureStringToByteArray("44 8b 4b ** 48 8b c8 4c 8b 43 ** 8b 53 ** 48 89 43 ** e8 ** ** ** ** 48 83 7b ** 00 ** ** e8") },
            { SignatureType.OodleNetwork1UDP_Encode, SignatureStringToByteArray("48 8b 57 ?? 4c 8b ce 48 8b 4f ?? 4c 8b c5 4c 89 ?? 24 20 e8" ) },
            { SignatureType.OodleMalloc, SignatureStringToByteArray("41 be 00 00 00 40 ba 10 00 00 00 49 8b ce ff 15" ) },
            { SignatureType.OodleFree, SignatureStringToByteArray("48 8b cb f3 ab 4d 85 c0 74 ?? 49 8b c8 ff 15" ) },
        };


        public unsafe Dictionary<SignatureType, int> Read(IntPtr library)
        {
            Dictionary<SignatureType, int> ret = new Dictionary<SignatureType, int>();
            List<SignatureType> signatureTypes = new List<SignatureType>((SignatureType[])Enum.GetValues(typeof(SignatureType)));

            NativeMethods.MODULEINFO info = new NativeMethods.MODULEINFO();
            if (!NativeMethods.GetModuleInformation(Process.GetCurrentProcess().Handle, library, out info, (uint)sizeof(NativeMethods.MODULEINFO)))
            {
                Trace.Write($"{nameof(SigScan)}.{nameof(Read)}: Cannot get module size for supplied library.");
                return ret;
            }

            IntPtr startAddress = info.lpBaseOfDll;
            IntPtr maxAddress = IntPtr.Add(info.lpBaseOfDll, (int)info.SizeOfImage);

            IntPtr currentAddress = startAddress;

            int maxBytePatternLength = _signatures.Values.Max(x => x.Length);

            for (int i = signatureTypes.Count - 1; i >= 0; i--)
            {
                int offset = GetFirstSignatureOccurrence(_signatures[signatureTypes[i]],
                    currentAddress, (int)info.SizeOfImage);

                if (offset > 0)
                {
                    int signature = GetSignaturefromOffset(currentAddress, startAddress, offset);
                    ret.Add(signatureTypes[i], signature);

                    Trace.WriteLine($"Found Signature [{signatureTypes[i]}] at offset [{signature:X8}]", "DEBUG-MACHINA");

                    _ = signatureTypes.Remove(signatureTypes[i]);
                }
            }

            // Missing one or more signatures
            if (signatureTypes.Any())
                for (int i = 0; i < signatureTypes.Count; i++)
                    Trace.WriteLine($"{nameof(SigScan)}.{nameof(Read)}: Missing Signature [{signatureTypes[i]}].", "DEBUG-MACHINA");

            return ret;
        }


        private unsafe int GetFirstSignatureOccurrence(int[] signature, IntPtr Start, int maxLength)
        {

            // loop through each byte in the block and scan for pattern
            for (int i = 0; i < maxLength - signature.Length; i++)
            {
                int numMatch = 0;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (signature[j] == -1)
                        numMatch++; // automatic match
                    else if (signature[j] != ((byte*)Start)[i + j])
                        break;
                    else
                        numMatch++; // byte is equal
                }

                if (numMatch == signature.Length)
                    return i + signature.Length;
            }

            return 0;
        }

        private int GetSignaturefromOffset(IntPtr currentAddress, IntPtr startAddress, int startIndex)
        {
            IntPtr matchAddress;

            // NOTE: 64-bit uses relative instruction pointer (RIP).

            // relative offset is only 32-bits
            matchAddress = (IntPtr)Marshal.ReadInt32(IntPtr.Add(currentAddress, startIndex));

            // add onto current address.
            matchAddress = new IntPtr(currentAddress.ToInt64() + startIndex + sizeof(int) + matchAddress.ToInt64());

            // subtract base address to get relative signature offset
            // note that this assumes the address is sane and will not overflow uint
            int offset = (int)(matchAddress.ToInt64() - startAddress.ToInt64());

            return offset;
        }
        private static int[] SignatureStringToByteArray(string pattern)
        {
            pattern = pattern.Replace(" ", "").Replace("??", "**");

            // convert the pattern into a parseable array
            int[] bytePattern = new int[pattern.Length / 2];
            for (int i = 0; i < (pattern.Length / 2); i++)
            {
                string tmpSearch = pattern.Substring(i * 2, 2);
                bytePattern[i] = tmpSearch == "**" ? -1 : Convert.ToByte(tmpSearch, 16);
            }

            return bytePattern;
        }
    }
}

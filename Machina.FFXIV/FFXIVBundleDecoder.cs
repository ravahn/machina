// Machina.FFXIV ~ FFXIVBundleDecoder.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO.Compression;

using System.Diagnostics;

namespace Machina.FFXIV
{
    public class FFXIVBundleDecoder
    {
        private byte[] _bundleBuffer = null;
        private byte[] _decompressionBuffer = new byte[1024 * 128];
        private int _allocated = 0;

        public Queue<Tuple<long, byte[]>> Messages = new Queue<Tuple<long, byte[]>>(20);

        public DateTime LastMessageTimestamp
            { get; set; } = DateTime.MinValue;

        public unsafe void StoreData(byte[] buffer)
        {
            // append buffer data
            if (_bundleBuffer == null)
            {
                _bundleBuffer = buffer;
                _allocated = buffer.Length;
            }
            else
            {
                // resize buffer if there is a remaining amount with a different length than the incoming buffer.
                if (_bundleBuffer.Length - _allocated != buffer.Length)
                    Array.Resize(ref _bundleBuffer, buffer.Length + _allocated);

                Array.Copy(buffer, 0, _bundleBuffer, _allocated, buffer.Length);
                _allocated += buffer.Length;
            }

            int offset = 0;
            while (offset < _allocated)
            {
                if (_allocated - offset < sizeof(FFXIVBundleHeader))
                {
                    if (offset > 0)
                    {
                        if (_allocated != offset)
                            Array.Copy(_bundleBuffer, offset, _bundleBuffer, 0, _allocated - offset);
                        _allocated -= offset;
                    }
                    return;
                }

                fixed (byte* ptr = _bundleBuffer)
                {
                    FFXIVBundleHeader header = *(FFXIVBundleHeader*)(ptr + offset);

                    if (header.magic0 != 0x41a05252)
                        if (header.magic0 != 0 && header.magic1 != 0 &&
                            header.magic2 != 0 && header.magic3 != 0)
                        {
                            if (LastMessageTimestamp != DateTime.MinValue)
                                Trace.WriteLine("FFXIVBundleDecoder: Invalid magic # in header:" + Utility.ByteArrayToHexString(_bundleBuffer, offset, 36));

                            offset = GetNextMagicNumberPos(_bundleBuffer, offset);
                            if (offset == -1)
                            {
                                //reset stream
                                _allocated = 0;
                                _bundleBuffer = null;
                                return;
                            }
                            continue;
                        }

                    // Exit if not all of the message is available yet.
                    if (header.length > _bundleBuffer.Length - offset)
                    {
                        if ((offset > 0) && (_allocated != offset))
                        { 
                            Array.Copy(_bundleBuffer, offset, _bundleBuffer, 0, _allocated - offset);
                            _allocated -= offset;
                        }
                        return;
                    }
                    int messageBufferSize;
                    byte[] message = DecompressFFXIVMessage(ref header, _bundleBuffer, offset, out messageBufferSize);

                    offset += header.length;
                    if (offset == _allocated)
                        _bundleBuffer = null;
                    if (messageBufferSize > 0)
                    {
                        int message_offset = 0;

                        fixed (byte* msgPtr = message)
                        {
                            for (int i = 0; i < header.message_count; i++)
                            {
                                ushort message_length = ((ushort*)(msgPtr + message_offset))[0];
                                byte[] data = new byte[message_length];
                                Array.Copy(message, message_offset, data, 0, data.Length);

                                Messages.Enqueue(new Tuple<long, byte[]>(
                                    (long)Utility.ntohq(header.epoch), data));

                                message_offset += message_length;
                                if (message_offset > messageBufferSize)
                                {
                                    Trace.WriteLine("FFXIVBundleDecoder: Bad message offset - offset=" + message_offset.ToString() + ", bufferSize=" + messageBufferSize.ToString() +
                                        ", data: " + Utility.ByteArrayToHexString(data, 0, 50));

                                    _allocated = 0;
                                    return;
                                }
                            }
                            LastMessageTimestamp = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        public unsafe Tuple<long, byte[]> GetNextFFXIVMessage()
        {
            if (Messages.Count > 0)
                return Messages.Dequeue();
            else
                return null;
        }

        private unsafe byte[] DecompressFFXIVMessage(ref FFXIVBundleHeader header, byte[] buffer, int offset, out int ffxivMessageSize)
        {
            ffxivMessageSize = 0;

            if (header.encoding == 0x0000 || header.encoding == 0x0001)
            {
                // uncompressed - copy to output buffer
                ffxivMessageSize = header.length - sizeof(FFXIVBundleHeader);
                for (int i = 0; i < ffxivMessageSize / 4; i++)
                {
                    // todo: use unsafe pointer operations
                    uint value = BitConverter.ToUInt32(buffer, offset + i * 4 + sizeof(FFXIVBundleHeader));
                    Array.Copy(BitConverter.GetBytes(value), 0, _decompressionBuffer, i * 4, 4);
                }

                return _decompressionBuffer;
            }

            if (header.encoding != 0x0101 && header.encoding != 0x0100)
            {
                Trace.WriteLine("FFXIVBundleDecoder: unknown encoding type: " + header.encoding.ToString("X4"));
                return null;
            }

            try
            {
                // inflate the packet using built-in .net function.  Note that the first two bytes of the data are skipped, since this 
                //  appears to be a standard zlib deflated buffer
                System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer, offset + 42, header.length - 42);
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    // todo: need more graceful way of determing decompressed size!
                    ffxivMessageSize = ds.Read(_decompressionBuffer, 0, _decompressionBuffer.Length);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("FFXIVBundleDecoder: Decompression error: " + ex.ToString());
                return null;
            }

            return _decompressionBuffer;
        }


        private unsafe int GetNextMagicNumberPos(byte[] buffer, int offset)
        {
            fixed (byte* ptr = buffer)
            {
                for (int i = 0; i < (buffer.Length - offset) / 4; i++)
                {
                    if (((int*)(ptr + offset + i))[0] == 0x5252a041)
                        return i;
                }
            }

            return -1;
        }
    }
}
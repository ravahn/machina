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
using System.Globalization;
using System.IO.Compression;
using Machina.FFXIV.Headers;
using Machina.Infrastructure;

namespace Machina.FFXIV
{
    public class FFXIVBundleDecoder
    {
        private byte[] _bundleBuffer;
        private readonly byte[] _decompressionBuffer = new byte[1024 * 128];
        private int _allocated;

        private FFXIVOodle _oodle;

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
                if (_allocated - offset < sizeof(Server_BundleHeader))
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
                    Server_BundleHeader header = *(Server_BundleHeader*)(ptr + offset);

                    if (header.magic0 != 0x41a05252)
                        if (header.magic0 != 0 && header.magic1 != 0 &&
                            header.magic2 != 0 && header.magic3 != 0)
                        {
                            if (LastMessageTimestamp != DateTime.MinValue)
                                Trace.WriteLine("FFXIVBundleDecoder: Invalid magic # in header:" + ConversionUtility.ByteArrayToHexString(_bundleBuffer, offset, 36), "DEBUG-MACHINA");

                            offset = ResetStream(offset);
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
                    byte[] message = DecompressFFXIVMessage(ref header, _bundleBuffer, offset, out int messageBufferSize);
                    if (message == null || messageBufferSize <= 0)
                    {
                        offset = ResetStream(offset);
                        continue;
                    }

                    offset += (int)header.length;
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
                                    (long)ConversionUtility.ntohq(header.epoch), data));

                                message_offset += message_length;
                                if (message_offset > messageBufferSize)
                                {
                                    Trace.WriteLine($"FFXIVBundleDecoder: Bad message offset - offset={message_offset}, bufferSize={messageBufferSize}, " +
                                        $"data: {ConversionUtility.ByteArrayToHexString(data, 0, 50)}", "DEBUG-MACHINA");

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
            return Messages.Count > 0 ? Messages.Dequeue() : null;
        }

        private bool _encodingError;

        private unsafe byte[] DecompressFFXIVMessage(ref Server_BundleHeader header, byte[] buffer, int offset, out int ffxivMessageSize)
        {
            ffxivMessageSize = 0;

            switch (header.compression)
            {
                case CompressionType.None:
                    // uncompressed - copy to output buffer
                    ffxivMessageSize = (int)header.length - sizeof(Server_BundleHeader);
                    for (int i = 0; i < ffxivMessageSize / 4; i++)
                    {
                        // todo: use unsafe pointer operations
                        uint value = BitConverter.ToUInt32(buffer, offset + (i * 4) + sizeof(Server_BundleHeader));
                        Array.Copy(BitConverter.GetBytes(value), 0, _decompressionBuffer, i * 4, 4);
                    }

                    break;
                case CompressionType.Zlib:
                    try
                    {
                        // inflate the packet using built-in .net function.  Note that the first two bytes of the data are skipped, since this 
                        //  appears to be a standard zlib deflated buffer
                        System.IO.MemoryStream ms = new System.IO.MemoryStream(buffer, offset + 42, (int)header.length - 42);
                        using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                        {
                            // todo: need more graceful way of determing decompressed size!
                            ffxivMessageSize = ds.Read(_decompressionBuffer, 0, _decompressionBuffer.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("FFXIVBundleDecoder: Decompression error: " + ex.ToString(), "DEBUG-MACHINA");
                        return null;
                    }

                    break;
                case CompressionType.Oodle:
                    try
                    {
                        if (_oodle == null)
                        {
                            _oodle = new FFXIVOodle();
                            _oodle.Initialize();
                        }

                        bool success = _oodle.Decompress(
                            buffer,
                            offset + sizeof(Server_BundleHeader),
                            (int)header.length - sizeof(Server_BundleHeader),
                            _decompressionBuffer,
                            (int)header.uncompressed_length);
                        if (success)
                        {
                            ffxivMessageSize = (int)header.uncompressed_length;
                        }
                        else
                        {
                            Trace.WriteLine("FFXIVBundleDecoder: Oodle Decompression failure.", "DEBUG-MACHINA");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("FFXIVBundleDecoder: Oodle Decompression error: " + ex.ToString(), "DEBUG-MACHINA");
                        return null;
                    }
                    break;
                default:
                    if (!_encodingError)
                        Trace.WriteLine($"FFXIVBundleDecoder: unknown bundle: version - {header.version.ToString("X2", CultureInfo.InvariantCulture)}; compression - {header.compression}", "DEBUG-MACHINA");
                    _encodingError = true;
                    return null;
            }

            return _decompressionBuffer;
        }

        private unsafe int ResetStream(int offset)
        {
            offset = GetNextMagicNumberPos(_bundleBuffer, offset);
            if (offset == -1)
            {
                //reset stream
                _allocated = 0;
                _bundleBuffer = null;
            }

            return offset;
        }

        private static unsafe int GetNextMagicNumberPos(byte[] buffer, int currentOffset)
        {
            fixed (byte* ptr = buffer)
            {
                for (int nextOffset = currentOffset + 1; nextOffset <= buffer.Length - 4; nextOffset++)
                {
                    if (((int*)(ptr + nextOffset))[0] == 0x5252a041)
                        return nextOffset;
                }
            }

            return -1;
        }
    }
}

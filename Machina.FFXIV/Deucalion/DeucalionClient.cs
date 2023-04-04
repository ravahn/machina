// Copyright © 2023 Ravahn - All Rights Reserved
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
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Machina.FFXIV.Headers;

namespace Machina.FFXIV.Deucalion
{
    public class DeucalionClient : IDisposable
    {
        private enum DeucalionChannel : uint
        {
            /// <summary>
            ///   Currently unimplemented.
            /// </summary>
            Lobby = 0,
            /// <summary>
            ///    RecvZonePacket: "49 8B 40 10 4C 8B 50 38" (as of global version 6.31h)
            /// </summary>
            Zone = 1,
            /// <summary>
            ///  Currently unimplemented
            /// </summary>
            Chat = 2
        }

        private enum DeucalionOpcode : byte
        {
            /// <summary>
            /// Used for passing debug text messages.
            /// </summary>
            Debug = 0,
            /// <summary>
            /// Used to maintain a connection between client and the hook server. The hook will echo a "pong" with the same op when it receives a ping
            /// </summary>
            Ping = 1,
            /// <summary>
            /// Used to signal the hook to unload itself from the host process.   
            /// </summary>
            Exit = 2,
            /// <summary>
            ///  When sent from the hook, contains the FFXIV message received by the host process. 
            /// </summary>
            Recv = 3,
            /// <summary>
            /// When sent from Deucalion, contains the FFXIV packet sent by the host process.
            /// </summary>
            Send = 4,
            /// <summary>
            /// Used to configure per-subscriber filtering for packets.
            /// </summary>
            Option = 5,
            /// <summary>
            /// When sent from Deucalion, contains the FFXIV non-IPC segment received by the host process.
            /// </summary>
            RecvOther = 6,
            /// <summary>
            /// When sent from Deucalion, contains the FFXIV non-IPC segment sent by the host process.
            /// </summary>
            SendOther = 7

        };

        private enum DeucalionFilter : byte
        {
            /// <summary>
            /// 	Allows received Lobby packets.
            /// </summary>
            AllowReceivedLobby = 1 << 0,
            /// <summary>
            /// 	Allows received Zone packets.
            /// </summary>
            AllowReceivedZone = 1 << 1,
            /// <summary>
            /// Allows received Chat packets.
            /// </summary>
            AllowReceivedChat = 1 << 2,
            /// <summary>
            /// 	Allows sent Lobby packets.
            /// </summary>
            AllowSentLobby = 1 << 3,
            /// <summary>
            /// 	Allows sent Zone packets.
            /// </summary>
            AllowSentZone = 1 << 4,
            /// <summary>
            /// 	Allows sent Chat packets.
            /// </summary>
            AllowSentChat = 1 << 5,
            /// <summary>
            ///     Allows other packet types or channels.
            /// </summary>
            AllowOther = 1 << 6
        }

        /// <summary>
        /// This defines the header data sent to/from the named pipe
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DeucalionHeader
        {
            public int Length;
            public DeucalionOpcode Opcode;
            public DeucalionChannel channel;
        }

        /// <summary>
        /// This is a helper class for processing the result sent back from the named pipe.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DeucalionMessage
        {
            public DeucalionHeader header;
            public byte[] data;
            public string debug;
        }

        /// <summary>
        /// This defines the structure of message header data sent back from the named pipe
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DeucalionSegment
        {
            public uint source_actor;
            public uint target_actor;
            public long timestamp;
            public ushort reserved; //0x0014
            public ushort type; //opcode
            public ushort padding;
            public ushort server;
            public uint seconds;
            public uint padding1;
        }

        private NamedPipeClientStream _clientStream;

        private CancellationTokenSource _tokenSource;
        private Task _monitorTask;

        private DateTime _lastLoopError;
        private bool disposedValue;

        private readonly byte[] _streamBuffer = new byte[short.MaxValue * 2];
        private int _streamBufferIndex;

        public delegate void MessageReceivedHandler(byte[] message);
        public MessageReceivedHandler MessageReceived;

        public delegate void MessageSentHandler(byte[] message);
        public MessageSentHandler MessageSent;

        public void OnMessageReceived(byte[] message)
        {
            MessageReceived?.Invoke(message);
        }

        public void OnMessageSent(byte[] message)
        {
            MessageSent?.Invoke(message);
        }

        public unsafe void Connect(int processId)
        {
            try
            {
                _clientStream = new NamedPipeClientStream(".", $"deucalion-{processId}");

                _tokenSource = new CancellationTokenSource();

                _clientStream.Connect(3000);
                if (!_clientStream.IsConnected)
                {
                    Trace.WriteLine($"DeucalionClient: Unable to connect to named pipe deucalion-{processId}.", "DEBUG-MACHINA");
                    return;
                }

                byte[] buffer = new byte[short.MaxValue];

                // Expect a result after initial connection
                DeucalionMessage result = ReadPipe(buffer, _tokenSource.Token).FirstOrDefault();
                if (result.header.Opcode != DeucalionOpcode.Debug || !result.debug.StartsWith("SERVER HELLO", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine($"DeucalionClient: Named pipe connected, but received unexpected response: ({result.header.Opcode} {result.debug}).", "DEBUG-MACHINA");
                    return;
                }

                // Set opcode filter to get recv/send for zone packets.  Assume it was processed successfully.
                WritePipe(new DeucalionMessage()
                {
                    header = new DeucalionHeader()
                    {
                        channel = (DeucalionChannel)(DeucalionFilter.AllowReceivedZone | DeucalionFilter.AllowSentZone),
                        Opcode = DeucalionOpcode.Option
                    },
                    data = Array.Empty<byte>()
                }, _tokenSource.Token);

                if (result.debug.Contains("REQUIRES SIG"))
                {
                    Trace.WriteLine("DeucalionClient: Named Pipe connected, but requires updated signature.  Cannot find network data.");
                    return;
                    ////Send named pipe the signature payload.  Note: this is unnecessary until the signature breaks.
                    //string signature = "E8 $ { ' } 4C 8B 43 10 41 8B 40 18";
                    //WritePipe(new DeucalionMessage()
                    //{
                    //    header = new DeucalionHeader()
                    //    {
                    //        channel = FFXIVChannel.Zone,
                    //        Opcode = FFXIVOpcodes.Recv
                    //    },
                    //    data = Encoding.ASCII.GetBytes(signature)
                    //});
                    //// No need to parse result, it will be logged via debug output.
                }

                // Set client nickname.  Also assume it was processed successfully.
                WritePipe(new DeucalionMessage()
                {
                    header = new DeucalionHeader()
                    {
                        channel = (DeucalionChannel)9000,
                        Opcode = DeucalionOpcode.Debug,
                    },
                    data = Encoding.UTF8.GetBytes("FFXIV_ACT_Plugin")
                }, _tokenSource.Token);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"DeucalionClient: Exception while setting up connection with Deucalion named pipe.  Data will not be logged.  {ex}", "DEBUG-MACHINA");
                return;
            }

            _monitorTask = Task.Run(() => ProcessReadLoop(_tokenSource.Token));
        }

        private void ProcessReadLoop(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[short.MaxValue];
                _streamBufferIndex = 0;
                DateTime lastClientPing = DateTime.Now;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // send ping
                        if (DateTime.Now.Subtract(lastClientPing).TotalMilliseconds > 1000)
                        {
                            lastClientPing = DateTime.Now;
                            WritePipe(new DeucalionMessage()
                            {
                                header = new DeucalionHeader()
                                {
                                    channel = DeucalionChannel.Zone,
                                    Opcode = DeucalionOpcode.Ping
                                }
                            }, token);
                        }
                        DeucalionMessage[] messages = ReadPipe(buffer, token);
                        if (messages == Array.Empty<DeucalionMessage>())
                        {
                            Task.Delay(10, token).Wait(token);
                            continue;
                        }

                        foreach (DeucalionMessage message in messages)
                        {
                            if (message.header.Opcode == DeucalionOpcode.Recv)
                                OnMessageReceived(message.data);
                            if (message.header.Opcode == DeucalionOpcode.Send)
                                OnMessageSent(message.data);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (DateTime.UtcNow.Subtract(_lastLoopError).TotalSeconds > 5)
                            Trace.WriteLine("DeucalionClient: Error in inner ProcessReadLoop. " + ex.ToString(), "DEBUG-MACHINA");
                        _lastLoopError = DateTime.UtcNow;
                    }

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Trace.WriteLine("DeucalionClient Error in outer ProcessReadLoop. " + ex.ToString(), "DEBUG-MACHINA");
            }
        }

        private unsafe DeucalionMessage[] ReadPipe(byte[] buffer, CancellationToken token)
        {
            List<DeucalionMessage> response = new List<DeucalionMessage>();

            // read all available data into the supplied buffer
            Task<int> readerTask = _clientStream.ReadAsync(buffer, 0, buffer.Length, token);
            readerTask.Wait(token);

            int read = 0;
            if (readerTask.IsCompleted && readerTask.Exception == null)
                read = readerTask.Result;
            else
            {
                Trace.WriteLine($"DeucalionClient: ReaderTask did not complete.  Exception: {readerTask.Exception}", "DEBUG-MACHINA");
            }
            if (read == 0)
                return response.ToArray();

            // for debugging: Trace.WriteLine($"DeucalionClient: Received {Infrastructure.ConversionUtility.ByteArrayToHexString(buffer, 0, read)}");

            // sanity check
            if (_streamBufferIndex + read > _streamBuffer.Length)
            {
                // buffer is full, but length suggests more data needed.  Reset stream.
                Trace.WriteLine($"DeucalionClient: Stream Buffer is full.  Discarding data and resetting stream.", "DEBUG-MACHINA");
                _streamBufferIndex = 0;
                return response.ToArray();
            }

            // copy data to stream buffer
            Array.Copy(buffer, 0, _streamBuffer, _streamBufferIndex, read);
            _streamBufferIndex += read;

            // process all data
            int index = 0;

            fixed (byte* ptr = _streamBuffer)
            {

                // loop through the buffer processing messages
                while (index < _streamBufferIndex)
                {
                    DeucalionHeader* messagePtr = (DeucalionHeader*)(ptr + index);

                    // sanity check that we have a complete payload
                    if (messagePtr->Length > _streamBufferIndex - index)
                    {
                        break;
                    }

                    // convert remaining payload to message, if any
                    DeucalionMessage newMessage = new DeucalionMessage()
                    {
                        header = *messagePtr,
                        data = messagePtr->Length > sizeof(DeucalionHeader) ? new byte[messagePtr->Length - sizeof(DeucalionHeader)] : Array.Empty<byte>(),
                        debug = (messagePtr->Opcode == DeucalionOpcode.Debug || messagePtr->Opcode == DeucalionOpcode.Ping) && messagePtr->Length > sizeof(DeucalionHeader) ?
                                Encoding.UTF8.GetString(_streamBuffer, sizeof(DeucalionHeader) + index, messagePtr->Length - sizeof(DeucalionHeader)) :
                                string.Empty
                    };
                    if (newMessage.data != Array.Empty<byte>())
                    {
                        Array.Copy(_streamBuffer, index + sizeof(DeucalionHeader), newMessage.data, 0, newMessage.data.Length);
                    }

                    // write out current message as string
                    //Trace.WriteLine($"DeucalionClient: Received ( Length = {messagePtr->Length}, Op = {messagePtr->Opcode}, Channel = {messagePtr->channel}, message = {newMessage.debug}");

                    switch (messagePtr->Opcode)
                    {
                        case DeucalionOpcode.Ping:
                            //Debug.WriteLine($"DeucalionClient: Received Ping on Channel {newMessage.header.channel}, message: {newMessage.debug}");
                            break;
                        case DeucalionOpcode.Debug:
                            Trace.WriteLine($"DeucalionClient: Debug Channel {newMessage.header.channel} Opcode {newMessage.header.Opcode} message: {newMessage.debug}", "DEBUG-MACHINA");
                            response.Add(newMessage);
                            break;
                        case DeucalionOpcode.Recv:
                            if (messagePtr->channel == DeucalionChannel.Zone)
                                response.Add(newMessage);
                            break;
                        case DeucalionOpcode.Send:
                            if (messagePtr->channel == DeucalionChannel.Zone)
                                response.Add(newMessage);
                            break;
                        case DeucalionOpcode.Exit:
                            Trace.WriteLine("DeucalionClient: Received exit opcode from injected code.", "DEBUG-MACHINA");
                            Disconnect();
                            break;
                        case DeucalionOpcode.Option:
                        default:
                            Trace.WriteLine($"DeucalionClient: Unexpected opcode {((DeucalionHeader*)ptr)->Opcode} from injected code.", "DEBUG-MACHINA");
                            break;
                    }

                    index += messagePtr->Length;
                }
            }

            // reset streambuffer
            if (index == _streamBufferIndex)
                _streamBufferIndex = 0;
            else
            {
                Array.Copy(_streamBuffer, index, _streamBuffer, 0, _streamBufferIndex - index);
                _streamBufferIndex -= index;
            }

            return response.ToArray();
        }


        private unsafe void WritePipe(DeucalionMessage message, CancellationToken token)
        {
            byte[] buffer = new byte[sizeof(DeucalionHeader) + (message.data?.Length ?? 0)];

            fixed (byte* ptr = buffer)
            {
                ((DeucalionHeader*)ptr)->Length = buffer.Length;
                ((DeucalionHeader*)ptr)->Opcode = message.header.Opcode;
                ((DeucalionHeader*)ptr)->channel = message.header.channel;
            }
            if (message.data != null)
                Array.Copy(message.data, 0, buffer, sizeof(DeucalionHeader), message.data.Length);

            Task writerTask = _clientStream.WriteAsync(buffer, 0, buffer.Length, token);

            writerTask.Wait(token);
            if (!writerTask.IsCompleted || writerTask.Exception != null)
            {
                Trace.WriteLine($"DeucalionClient: WriterTask did not complete.  Exception: {writerTask.Exception}", "DEBUG-MACHINA");
            }

            //Debug.WriteLine($"DeucalionClient: Sent Opcode {message.header.Opcode} to channel {message.header.channel}, total length {buffer.Length}");
        }

        public void Disconnect()
        {
            _tokenSource?.Cancel();

            if (_clientStream != null && _clientStream.IsConnected)
            {
             //_clientStream.Flush();
                _clientStream.Close();
            }
            _clientStream?.Dispose();
            _clientStream = null;

            _tokenSource?.Dispose();
            _tokenSource = null;
        }


        public static unsafe (long, byte[]) ConvertDeucalionFormatToPacketFormat(byte[] message)
        {
            // convert to public network wire structure
            byte[] convertedMessage = new byte[message.Length + sizeof(Server_MessageHeader) - sizeof(DeucalionSegment)];

            long epoch;
            fixed (byte* ptr = convertedMessage)
            {
                Server_MessageHeader* headerPtr = (Server_MessageHeader*)ptr;
                fixed (byte* ptr2 = message)
                {
                    DeucalionSegment* segmentPtr = (DeucalionSegment*)ptr2;

                    headerPtr->MessageLength = (uint)convertedMessage.Length;
                    headerPtr->LoginUserID = segmentPtr->target_actor;
                    headerPtr->ActorID = segmentPtr->source_actor;
                    headerPtr->Unknown2 = segmentPtr->reserved;
                    headerPtr->MessageType = segmentPtr->type;
                    headerPtr->Seconds = segmentPtr->seconds;

                    epoch = segmentPtr->timestamp;
                }
            }

            Array.Copy(message, sizeof(DeucalionSegment), convertedMessage, sizeof(Server_MessageHeader), message.Length - sizeof(DeucalionSegment));

            return (epoch, convertedMessage);
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_tokenSource != null)
                    {
                        _tokenSource.Cancel();
                        _tokenSource.Dispose();
                        _tokenSource = null;
                    }

                    if (_clientStream != null)
                    {
                        if (_clientStream.IsConnected)
                            _clientStream.Close();
                        _clientStream.Dispose();
                        _clientStream = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machina.FFXIV.Deucalion
{
    public class DeucalionClient : IDisposable
    {
        private enum FFXIVChannel : uint
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

        private enum FFXIVOpcodes : byte
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
            Recv = 3
        };

        /// <summary>
        /// This defines the header data sent to/from the named pipe
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DeucalionHeader
        {
            public int Length;
            public FFXIVOpcodes Opcode;
            public FFXIVChannel channel;
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
            public ushort reserved; //0x0014
            public ushort type; //opcode
            public ushort padding;
            public ushort server;
            public uint timestamp; // seconds
            public uint padding1;
        }

        private NamedPipeClientStream _clientStream;

        private CancellationTokenSource _tokenSource;
        private Task _monitorTask;

        private DateTime _lastLoopError;
        private bool disposedValue;


        public delegate void MessageReceivedHandler(byte[] message);
        public MessageReceivedHandler MessageReceived;
        public void OnMessageReceived(byte[] message)
        {
            MessageReceived?.Invoke(message);
        }

        public unsafe void Connect(int processId)
        {
            try
            {
                _clientStream = new NamedPipeClientStream(".", $"deucalion-{processId}");

                _clientStream.Connect(3000);
                if (!_clientStream.IsConnected)
                {
                    Trace.WriteLine($"DeucalionClient: Unable to connect to named pipe deucalion-{processId}.", "DEBUG-MACHINA");
                    return;
                }

                byte[] buffer = new byte[short.MaxValue];

                // Expect a result after initial connection
                DeucalionMessage result = ReadPipe(buffer).FirstOrDefault();
                if (result.header.Opcode != FFXIVOpcodes.Debug || result.debug != "SERVER HELLO")
                {
                    Trace.WriteLine($"DeucalionClient: Named pipe connected, but received unexpected response: ({result.header.Opcode} {result.debug}).", "DEBUG-MACHINA");
                    return;
                }

                // Sent named pipe the signature payload
                string signature = "49 8B 40 10 4C 8B 50 38";
                WritePipe(new DeucalionMessage()
                {
                    header = new DeucalionHeader()
                    {
                        channel = FFXIVChannel.Zone,
                        Opcode = FFXIVOpcodes.Recv
                    },
                    data = Encoding.ASCII.GetBytes(signature)
                });

                // Expect back either OK or an already-initialized error
                result = ReadPipe(buffer).FirstOrDefault();
                if (result.header.Opcode != FFXIVOpcodes.Debug || result.debug != "OK")
                {
                    // log but do not exit
                    Trace.WriteLine($"DeucalionClient: Received initial response but cannot verify hook was successful.  response: ({result.debug}).", "DEBUG-MACHINA");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"DeucalionClient: Exception while setting up connection with Deucalion named pipe.  Data will not be logged.  {ex}", "DEBUG-MACHINA");
                return;
            }
 
            _tokenSource = new CancellationTokenSource();

            _monitorTask = Task.Run(() => ProcessReadLoop(_tokenSource.Token));
        }

        private void ProcessReadLoop(CancellationToken token)
        {
            try
            {
                byte[] buffer = new byte[short.MaxValue];

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        DeucalionMessage[] messages = ReadPipe(buffer);
                        foreach (DeucalionMessage message in messages)
                            if (message.header.Opcode == FFXIVOpcodes.Recv)
                                OnMessageReceived(message.data);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        if (DateTime.UtcNow.Subtract(_lastLoopError).TotalSeconds > 5)
                            Trace.WriteLine("DeucalionClient: Error in inner ProcessReadLoop. " + ex.ToString(), "DEBUG-MACHINA");
                        _lastLoopError = DateTime.UtcNow;
                    }

                    Task.Delay(30, token).Wait(token);
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

        private unsafe DeucalionMessage[] ReadPipe(byte[] buffer)
        {
            List<DeucalionMessage> response = new List<DeucalionMessage>();

            // read all available data into the supplied buffer
            int read = _clientStream.Read(buffer, 0, buffer.Length);
            if (read == 0)
                return response.ToArray();

            int index = 0;

            fixed (byte* ptr = buffer)
            {
                // for debugging: Trace.WriteLine($"DeucalionClient: Received {Infrastructure.ConversionUtility.ByteArrayToHexString(buffer, 0, read)}");

                // loop through the buffer processing messages
                while (index < read)
                {
                    DeucalionHeader* messagePtr = (DeucalionHeader*)(ptr + index);

                    // sanity check
                    if (messagePtr->Length > read - index)
                    {
                        Trace.WriteLine($"DeucalionClient: Message length {messagePtr->Length} exceeds remaining buffer size {read - index}.  Discarding data.", "DEBUG-MACHINA");
                        break;
                    }

                    // convert remaining payload to message, if any
                    DeucalionMessage newMessage = new DeucalionMessage() 
                    {
                        header = *messagePtr, 
                        data = messagePtr->Length > sizeof(DeucalionHeader) ? new byte[messagePtr->Length - sizeof(DeucalionHeader)] : Array.Empty<byte>(),
                        debug = (messagePtr->Opcode == FFXIVOpcodes.Debug || messagePtr->Opcode == FFXIVOpcodes.Ping) && messagePtr->Length > sizeof(DeucalionHeader) ?
                                Encoding.UTF8.GetString(buffer, sizeof(DeucalionHeader) + index, messagePtr->Length - sizeof(DeucalionHeader)) : 
                                string.Empty
                    };
                    if (newMessage.data != Array.Empty<byte>())
                    {
                        Array.Copy(buffer, index + sizeof(DeucalionHeader), newMessage.data, 0, newMessage.data.Length);
                    }

                    // write out current message as string
                    //Trace.WriteLine($"DeucalionClient: Received ( Length = {messagePtr->Length}, Op = {messagePtr->Opcode}, Channel = {messagePtr->channel}, message = {newMessage.debug}");

                    switch (messagePtr->Opcode)
                    {
                        case FFXIVOpcodes.Ping:
                            Trace.WriteLine($"DeucalionClient: Ping message: {newMessage.debug}");
                            break;
                        case FFXIVOpcodes.Debug:
                            response.Add(newMessage);
                            break;
                        case FFXIVOpcodes.Recv:

                            if (messagePtr->channel == FFXIVChannel.Zone)
                                response.Add(newMessage);
                            break;
                        case FFXIVOpcodes.Exit:
                            Trace.WriteLine("DeucalionClient: Received exit opcode from injected code.", "DEBUG-MACHINA");
                            Disconnect();
                            break;
                        default:
                            Trace.WriteLine($"DeucalionClient: Unexpected opcode {((DeucalionHeader*)ptr)->Opcode} from injected code.", "DEBUG-MACHINA");
                            break;
                    }

                    index += messagePtr->Length;
                }
            }

            return response.ToArray();
        }


        private unsafe void WritePipe(DeucalionMessage message)
        {
            byte[] buffer = new byte[sizeof(DeucalionHeader) + message.data.Length];
            fixed (byte* ptr = buffer)
            {
                ((DeucalionHeader*)ptr)->Length = sizeof(DeucalionHeader) + message.data.Length;
                ((DeucalionHeader*)ptr)->Opcode = message.header.Opcode;
                ((DeucalionHeader*)ptr)->channel = message.header.channel;
            }
            Array.Copy(message.data, 0, buffer, sizeof(DeucalionHeader), message.data.Length);

            _clientStream.Write(buffer, 0, buffer.Length);
        }

        public void Disconnect()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;

            if (_clientStream != null && _clientStream.IsConnected)
                _clientStream.Close();
            _clientStream?.Dispose();
            _clientStream = null;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_clientStream != null)
                    {
                        if (_clientStream.IsConnected)
                            _clientStream.Close();
                        _clientStream.Dispose();
                        _clientStream = null;
                    }

                    if (_tokenSource != null)
                    {
                        _tokenSource?.Cancel();
                        _tokenSource?.Dispose();
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

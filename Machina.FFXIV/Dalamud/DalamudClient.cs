using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Machina.FFXIV.Dalamud
{
    public class DalamudClient : IDisposable
    {
        public static ConcurrentQueue<(long, byte[])> MessageQueue;

        public delegate void MessageReceivedHandler(long epoch, byte[] message);
        public MessageReceivedHandler MessageReceived;

        private CancellationTokenSource _tokenSource;

        private DateTime _lastLoopError;


        public void OnMessageReceived(long epoch, byte[] message)
        {
            MessageReceived?.Invoke(epoch, message);
        }

        public void Connect()
        {
            MessageQueue = new ConcurrentQueue<(long, byte[])>();

            _tokenSource = new CancellationTokenSource();

             Task.Run(() => ProcessReadLoop(_tokenSource.Token));
        }

        private void ProcessReadLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        while (MessageQueue.TryDequeue(out var messageInfo))
                        {
                            OnMessageReceived(messageInfo.Item1, messageInfo.Item2);
                        }

                        Task.Delay(10, token).Wait(token);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        if (DateTime.UtcNow.Subtract(_lastLoopError).TotalSeconds > 5)
                            Trace.WriteLine("DalamudClient: Error in inner ProcessReadLoop. " + ex.ToString(), "DEBUG-MACHINA");
                        _lastLoopError = DateTime.UtcNow;
                    }

                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Trace.WriteLine("DalamudClient: Error in outer ProcessReadLoop. " + ex.ToString(), "DEBUG-MACHINA");
            }
        }

        public void Disconnect()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
            MessageQueue?.Clear();
            MessageQueue = null;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            Disconnect();
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

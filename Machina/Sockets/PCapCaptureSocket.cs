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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Machina.Infrastructure;
using static Machina.Sockets.PcapInterop;

namespace Machina.Sockets
{
    public class PCapCaptureSocket : ICaptureSocket
    {
        private readonly ConcurrentQueue<Tuple<byte[], int>> _pendingBuffers = new ConcurrentQueue<Tuple<byte[], int>>();
        private PcapDeviceState _activeDevice;
        private Task _monitorTask;
        private CancellationTokenSource _tokenSource;
        private bool _disposedValue;

        public void StartCapture(uint localAddress, uint remoteAddress = 0)
        {
            StopCapture();

            PcapDevice device = PcapDevice.GetAllDevices().FirstOrDefault(x =>
                x.Addresses.Contains(localAddress));

            if (string.IsNullOrWhiteSpace(device?.Name))
            {
                Trace.WriteLine($"PCapCaptureSocket: IP [{new IPAddress(localAddress)} selected but unable to find corresponding WinPCap device.", "DEBUG-MACHINA");
                return;
            }

            string filterText = "ip and tcp";
            if (remoteAddress > 0)
                filterText += " and host " + new IPAddress(remoteAddress).ToString();

            IntPtr filter = Marshal.AllocHGlobal(12);

            try
            {
                _activeDevice = new PcapDeviceState()
                {
                    Device = device,
                    Handle = IntPtr.Zero
                };

                StringBuilder errorBuffer = new StringBuilder(PCAP_ERRBUF_SIZE);

                // flags=0 turns off promiscous mode, which is not needed or desired.
                _activeDevice.Handle = pcap_open(device.Name, 65536, 0, 500, IntPtr.Zero, errorBuffer);
                if (_activeDevice.Handle == IntPtr.Zero)
                    throw new PcapException($"PCapCaptureSocket: Cannot open pcap interface [{device.Name}].  Error: {errorBuffer}");

                // check data link type
                _activeDevice.LinkType = pcap_datalink(_activeDevice.Handle);
                if (_activeDevice.LinkType != DLT_EN10MB && _activeDevice.LinkType != DLT_NULL)
                    throw new PcapException($"PCapCaptureSocket: Interface [{device.Description}] does not appear to support Ethernet.");

                // create filter
                if (pcap_compile(_activeDevice.Handle, filter, filterText, 1, 0) != 0)
                    throw new PcapException("PCapCaptureSocket: Unable to create TCP packet filter.");

                // apply filter
                if (pcap_setfilter(_activeDevice.Handle, filter) != 0)
                    throw new PcapException("PCapCaptureSocket: Unable to apply TCP packet filter.");

                // free filter memory
                pcap_freecode(filter);

                // Start monitoring task
                _tokenSource = new CancellationTokenSource();
                _monitorTask = Task.Run(() => RunCaptureLoop(_tokenSource.Token));
            }
            catch (Exception ex)
            {
                // clean up device
                if (_activeDevice.Handle != IntPtr.Zero)
                    pcap_close(_activeDevice.Handle);

                _activeDevice = null;

                throw new PcapException($"PCapCaptureSocket: Unable to open winpcap device [{device.Name}].", ex);
            }
            finally
            {
                // free memory
                Marshal.FreeHGlobal(filter);
            }
        }

        public void StopCapture()
        {
            try
            {
                _tokenSource?.Cancel();

                // stop pcap capture thread
                if (_monitorTask != null)
                    if (!_monitorTask.Wait(100) || _monitorTask.Status == TaskStatus.Running)
                        Trace.Write("PCapCaptureSocket: Task cannot be stopped.", "DEBUG-MACHINA");

                _monitorTask?.Dispose();
                _monitorTask = null;
                _tokenSource?.Dispose();
                _tokenSource = null;

                FreeBuffers();

                if (_activeDevice != null)
                {
                    if (_activeDevice.Handle != IntPtr.Zero)
                        pcap_close(_activeDevice.Handle);

                    _activeDevice.Handle = IntPtr.Zero;
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PCapCaptureSocket: Exception cleaning up RawPCap class. {ex}", "DEBUG-MACHINA");
            }
        }


        private void FreeBuffers()
        {
            while (_pendingBuffers.TryDequeue(out Tuple<byte[], int> next))
                BufferCache.ReleaseBuffer(next.Item1);
        }

        public CapturedData Receive()
        {
            if (_pendingBuffers.TryDequeue(out Tuple<byte[], int> next))
                return new CapturedData { Buffer = next.Item1, Size = next.Item2 };

            return new CapturedData { Buffer = null, Size = 0 };
        }

        private unsafe void RunCaptureLoop(CancellationToken token)
        {
            bool bExceptionLogged = false;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_activeDevice == null)
                    {
                        Task.Delay(100, token).Wait(token);
                        continue;
                    }

                    IntPtr packetDataPtr = IntPtr.Zero;
                    IntPtr packetHeaderPtr = IntPtr.Zero;

                    int layer2Length = _activeDevice.LinkType == DLT_EN10MB ? 14 : 4; // 14 for ethernet, 4 for loopback

                    // note: buffer returned by pcap_next_ex is static and owned by pcap library, does not need to be freed.
                    int status = pcap_next_ex(_activeDevice.Handle, ref packetHeaderPtr, ref packetDataPtr);
                    if (status == 0) // 500ms timeout
                        continue;
                    else if (status == -1) // error
                    {
                        string error = Marshal.PtrToStringAnsi(pcap_geterr(_activeDevice.Handle));
                        if (!bExceptionLogged)
                            Trace.WriteLine($"PCapCaptureSocket: Error during pcap_loop. {error}", "DEBUG-MACHINA");

                        bExceptionLogged = true;

                        Task.Delay(100, token).Wait(token);
                    }
                    else if (status != 1) // anything else besides success
                    {
                        if (!bExceptionLogged)
                            Trace.WriteLine($"PCapCaptureSocket: Unknown response code [{status}] from pcap_next_ex.", "DEBUG-MACHINA");

                        bExceptionLogged = true;

                        Task.Delay(100, token).Wait(token);
                    }
                    else
                    {
                        pcap_pkthdr packetHeader = *(pcap_pkthdr*)packetHeaderPtr;
                        if (packetHeader.caplen <= layer2Length)
                            continue;

                        byte[] buffer = BufferCache.AllocateBuffer();

                        // prepare data - skip the 14-byte ethernet header
                        int allocatedSize = (int)packetHeader.caplen - layer2Length;
                        if (allocatedSize > buffer.Length)
                            Trace.WriteLine($"PCapCaptureSocket: packet length too large: {allocatedSize} ", "DEBUG-MACHINA");
                        else
                        {
                            Marshal.Copy(packetDataPtr + layer2Length, buffer, 0, allocatedSize);

                            _pendingBuffers.Enqueue(new Tuple<byte[], int>(buffer, allocatedSize));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!bExceptionLogged)
                        Trace.WriteLine("PCapCaptureSocket: Exception during RunCaptureLoop. " + ex.ToString(), "DEBUG-MACHINA");

                    bExceptionLogged = true;

                    // add sleep 
                    Task.Delay(100, token).Wait(token);
                }
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _tokenSource?.Dispose();
                    _monitorTask?.Dispose();

                    FreeBuffers();
                }
                _disposedValue = true;
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

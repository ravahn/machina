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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Machina.Infrastructure.TCPNetworkMonitorConfig;
using static Machina.Sockets.PcapInterop;

namespace Machina.Sockets
{
    public class PCapCaptureSocket : ICaptureSocket
    {
        private readonly string _source;
        private readonly string _file;
        private pcap_rmtauth _auth;
        private readonly ConcurrentQueue<Tuple<byte[], int>> _pendingBuffers = new ConcurrentQueue<Tuple<byte[], int>>();
        private PcapDeviceState _activeDevice;
        private Task _monitorTask;
        private CancellationTokenSource _tokenSource;
        private bool _disposedValue;

        public PCapCaptureSocket() : this(new RPCapConf())
        {
        }

        public PCapCaptureSocket(RPCapConf config)
        {
            _auth = new pcap_rmtauth();
            _auth.username = config.username;
            _auth.password = config.password;
            _auth.type = string.IsNullOrEmpty(config.username) ? RPCAP_RMTAUTH_NULL : RPCAP_RMTAUTH_PWD;
            _file = config.file;
            _source = BuildSource(config.host, config.port);
            Trace.WriteLine($"PCapCaptureSocket: Capture source was set to [{_source}].", "DEBUG-MACHINA");
        }

        private string BuildSource(string host, int port)
        {
            if (!string.IsNullOrEmpty(_file))
                return new StringBuilder($"file://{System.IO.Path.GetDirectoryName(_file)}", PCAP_BUF_SIZE).ToString();
            StringBuilder source = new StringBuilder("rpcap://", PCAP_BUF_SIZE);
            if (string.IsNullOrEmpty(host))
                return source.ToString();
            _ = source.Append(host.Contains(":") ? $"[{host}]" : host);
            _ = source.Append(port > 0 ? $":{port}/" : ":2002/");
            return source.ToString();
        }

        private PcapDevice GetDevice(uint localAddress)
        {
            IList<PcapDevice> devices = PcapDevice.GetAllDevices(_source, ref _auth);
            PcapDevice device;

            if (_source.StartsWith("file://", StringComparison.InvariantCultureIgnoreCase))
            {
                device = devices.FirstOrDefault(x => x.Name.Contains(_file));
                if (string.IsNullOrWhiteSpace(device?.Name))
                {
                    Trace.WriteLine($"PCapCaptureSocket: File [{_file}] does not exist or is not in a valid pcap format.", "DEBUG-MACHINA");
                    return null;
                }
                return device;
            }

            device = devices.FirstOrDefault(x => x.Addresses.Contains(localAddress));

            if (string.IsNullOrWhiteSpace(device?.Name))
            {
                Trace.WriteLine($"PCapCaptureSocket: IP [{new IPAddress(localAddress)}] selected but unable to find corresponding local WinPCap device.", "DEBUG-MACHINA");
                device = devices.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(device?.Name))
                {
                    Trace.WriteLine($"PCapCaptureSocket: Cannot find any WinPCap devices.", "DEBUG-MACHINA");
                    return null;
                }
                Trace.WriteLine($"PCapCaptureSocket: Using pcap interface [{device.Name}] as fallback.", "DEBUG-MACHINA");
            }
            return device;
        }

        public void StartCapture(uint localAddress, uint remoteAddress = 0)
        {
            StopCapture();

            PcapDevice device = GetDevice(localAddress);
            if (device == null)
                return;

            string filterText = "ip and tcp";
            if (remoteAddress > 0)
                filterText += " and host " + new IPAddress(remoteAddress).ToString();

            bpf_program filter = new bpf_program();

            try
            {
                _activeDevice = new PcapDeviceState()
                {
                    Device = device,
                    Handle = IntPtr.Zero
                };

                StringBuilder errorBuffer = new StringBuilder(PCAP_ERRBUF_SIZE);

                // flags=0 turns off promiscous mode, which is not needed or desired.
                _activeDevice.Handle = pcap_open(device.Name, 65536, PCAP_OPENFLAG_MAX_RESPONSIVENESS | PCAP_OPENFLAG_NOCAPTURE_RPCAP, 100, ref _auth, errorBuffer);
                if (_activeDevice.Handle == IntPtr.Zero)
                    throw new PcapException($"PCapCaptureSocket: Cannot open pcap interface [{device.Name}].  Error: {errorBuffer}");

                // check data link type
                _activeDevice.LinkType = pcap_datalink(_activeDevice.Handle);
                if (_activeDevice.LinkType != DLT_EN10MB && _activeDevice.LinkType != DLT_RAW && _activeDevice.LinkType != DLT_NULL)
                    throw new PcapException($"PCapCaptureSocket: Interface [{device.Description}] does not appear to support Ethernet or raw IP.");

                // create filter
                if (pcap_compile(_activeDevice.Handle, ref filter, filterText, 1, 0) != 0)
                    throw new PcapException("PCapCaptureSocket: Unable to create TCP packet filter.");

                // apply filter
                if (pcap_setfilter(_activeDevice.Handle, ref filter) != 0)
                    throw new PcapException("PCapCaptureSocket: Unable to apply TCP packet filter.");

                // free filter memory
                pcap_freecode(ref filter);

                // Start monitoring task
                _tokenSource = new CancellationTokenSource();
                _monitorTask = Task.Run(() => RunCaptureLoop(_tokenSource.Token));
            }
            catch (Exception ex)
            {
                FreeDevice();
                throw new PcapException($"PCapCaptureSocket: Unable to open winpcap device [{device.Name}].", ex);
            }
        }

        public void StopCapture()
        {
            try
            {
                _tokenSource?.Cancel();

                // stop pcap capture thread
                if (_monitorTask != null)
                {
                    if (!_monitorTask.Wait(100) || _monitorTask.Status == TaskStatus.Running)
                        Trace.Write("PCapCaptureSocket: Task cannot be stopped.", "DEBUG-MACHINA");
                    else
                        _monitorTask.Dispose();
                }
                _monitorTask = null;
                _tokenSource?.Dispose();
                _tokenSource = null;

                FreeDevice();
                FreeBuffers();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PCapCaptureSocket: Exception cleaning up RawPCap class. {ex}", "DEBUG-MACHINA");
            }
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

                    int layer2Length = _activeDevice.LinkType == DLT_EN10MB ? 14 : _activeDevice.LinkType == DLT_RAW ? 0 : 4; // 14 for ethernet, 0 for raw IP, 4 for loopback

                    // note: buffer returned by pcap_next_ex is static and owned by pcap library, does not need to be freed.
                    int status = pcap_next_ex(_activeDevice.Handle, ref packetHeaderPtr, ref packetDataPtr);
                    if (status == 0) // 100ms timeout
                        continue;
                    else if (status == -1) // error
                    {
                        string error = Marshal.PtrToStringAnsi(pcap_geterr(_activeDevice.Handle));
                        if (!bExceptionLogged)
                            Trace.WriteLine($"PCapCaptureSocket: Error from pcap_next_ex. {error}", "DEBUG-MACHINA");

                        bExceptionLogged = true;

                        Task.Delay(100, token).Wait(token);
                    }
                    else if (status == -2) // no more packets in savefile
                    {
                        if (!bExceptionLogged)
                            Trace.WriteLine($"PCapCaptureSocket: pcap_next_ex has reached the end of {_activeDevice.Device.Name}.", "DEBUG-MACHINA");

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

                        // prepare data - skip the 14-byte ethernet header
                        int allocatedSize = (int)packetHeader.caplen - layer2Length;
                        if (allocatedSize > 0x1000000)
                            Trace.WriteLine($"PCapCaptureSocket: packet length too large: {allocatedSize} ", "DEBUG-MACHINA");
                        else
                        {
                            byte[] buffer = new byte[allocatedSize];
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

        private void FreeDevice()
        {
            if (_activeDevice != null)
            {
                if (_activeDevice.Handle != IntPtr.Zero)
                    pcap_close(_activeDevice.Handle);

                _activeDevice.Handle = IntPtr.Zero;

                _activeDevice = null;
            }

        }

        private void FreeBuffers()
        {
            while (_pendingBuffers.TryDequeue(out Tuple<byte[], int> _))
            {
                // do nothing
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

                    FreeDevice();
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

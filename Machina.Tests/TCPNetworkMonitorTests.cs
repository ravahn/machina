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

using System.Diagnostics;
using System.Threading.Tasks;
using Machina.Infrastructure;
using Machina.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.Tests
{
    [TestClass()]
    public class TCPNetworkMonitorTests
    {
        private int dataReceivedCount = 0;
        private int dataSentCount = 0;

        [TestMethod()]
        public void TCPNetworkMonitor_RawSocket_SendAndReceiveData()
        {
            TCPNetworkMonitor monitor = new TCPNetworkMonitor();
            monitor.Config.ProcessID = (uint)Process.GetCurrentProcess().Id;
            monitor.Config.MonitorType = NetworkMonitorType.RawSocket;
            monitor.DataReceivedEventHandler += (TCPConnection connection, byte[] data) => DataReceived();
            monitor.DataSentEventHandler += (TCPConnection connection, byte[] data) => DataSent();
            monitor.Config.UseRemoteIpFilter = false;

            monitor.Start();
            // start a dummy async download
            System.Net.WebClient client = new System.Net.WebClient();
            Task t = client.DownloadStringTaskAsync("http://www.google.com");
            t.Wait();

            t = client.DownloadStringTaskAsync("http://www.google.com");
            t.Wait();

            for (int i = 0; i < 100; i++)
            {
                if (dataSentCount > 1 && dataReceivedCount > 1)
                    break;

                System.Threading.Thread.Sleep(10);
            }

            monitor.Stop();

            Assert.IsTrue(dataReceivedCount >= 1);
            Assert.IsTrue(dataSentCount >= 1);
        }

        private void DataReceived()
        {
            dataReceivedCount++;
        }
        private void DataSent()
        {
            dataSentCount++;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }
    }
}

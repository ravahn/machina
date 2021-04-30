using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
            monitor.ProcessID = (uint)Process.GetCurrentProcess().Id;
            monitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
            monitor.DataReceived2 += (TCPConnection connection, byte[] data) => DataReceived(connection, data);
            monitor.DataSent2 += (TCPConnection connection, byte[] data) => DataSent(connection, data);
            monitor.UseSocketFilter = false;

            monitor.Start();
            // start a dummy async download
            System.Net.WebClient client = new System.Net.WebClient();
            Task t = client.DownloadStringTaskAsync("http://www.google.com");
            t.Wait();

            t = client.DownloadStringTaskAsync("http://www.google.com");
            t.Wait();

            for (int i=0;i<100;i++)
            {
                if (dataSentCount > 1 && dataReceivedCount > 1)
                    break;  

                System.Threading.Thread.Sleep(10);
            }

            monitor.Stop();
            
            Assert.IsTrue(dataReceivedCount >= 1);
            Assert.IsTrue(dataSentCount >= 1);
        }

        private void DataReceived(TCPConnection connection, byte[] data)
        {
            dataReceivedCount++;
        }
        private void DataSent(TCPConnection connection, byte[] data)
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
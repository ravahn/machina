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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Machina.Infrastructure;
using Machina.Sockets;
using Machina.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.Tests
{
    [TestClass]
    public class RawCaptureSocketTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }

        /// <summary>
        /// This tests retrieving data twice from the standard win32 local raw socket
        ///   It requires administrative permissions to execute, and requires that any
        ///   firewalls permit the test runner process to access data.
        /// </summary>
        [TestMethod()]
        public void RawSocket_GetDataTwiceTest()
        {
            string ip = InterfaceHelper.GetNetworkInterfaceIPs().FirstOrDefault();
            Assert.IsTrue(!string.IsNullOrEmpty(ip), "Unable to locate a network interface to test RawSocket.");

            RawCaptureSocket sut = new RawCaptureSocket();
            uint ipLong = ConversionUtility.IPStringToUint(ip);

            // start an async download
            System.Net.WebClient client = new System.Net.WebClient();
            Task t = client.DownloadStringTaskAsync("http://www.google.com");

            int receivedCount = 0;

            try
            {
                sut.StartCapture(ipLong);
                t.Wait();
                for (int i = 0; i < 10000; i++)
                {

                    CapturedData data = sut.Receive();
                    if (data.Size > 0)
                        receivedCount++;
                    else
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    if (receivedCount == 2)
                        break;
                }

            }
            finally
            {
                sut.StopCapture();
            }
            Assert.AreEqual(2, receivedCount);
        }


    }
}

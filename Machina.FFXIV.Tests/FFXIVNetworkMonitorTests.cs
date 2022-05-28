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

using Machina.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.FFXIV.Tests
{
    [TestClass]
    public class FFXIVNetworkMonitorTests
    {
        [TestMethod]
        public void FFXIVNetworkMonitor_ReceiveGameData()
        {
            bool dataReceived = false;
            bool dataSent = false;

            FFXIVNetworkMonitor sut = new();
            sut.MessageReceivedEventHandler = (TCPConnection connection, long epoch, byte[] data) =>
                { dataReceived = true; };
            sut.MessageSentEventHandler = (TCPConnection connection, long epoch, byte[] data) =>
                { dataSent = true; };
            sut.Start();
            for (int i = 0; i < 500; i++)
            {
                if (dataReceived && dataSent)
                    break;
                System.Threading.Thread.Sleep(10);
            }
            sut.Stop();

            Assert.IsTrue(dataReceived);
            Assert.IsTrue(dataSent);

        }
    }
}

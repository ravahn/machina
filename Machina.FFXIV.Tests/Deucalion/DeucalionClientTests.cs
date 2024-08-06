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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Machina.FFXIV.Deucalion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.FFXIV.Tests.Deucalion
{
    [TestClass()]
    public class DeucalionClientTests
    {
        [TestMethod()]
        [TestCategory("Integration")]
        public void DeucalionClientTest()
        {
            // note: this requires starting FFXIV so it can be injected.
            int processId = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault()?.Id ?? 0;

            bool isValid = DeucalionInjector.ValidateLibraryChecksum();
            Assert.IsTrue(isValid);
            bool injectionResult = DeucalionInjector.InjectLibrary(processId);
            Assert.IsTrue(injectionResult);

            DeucalionClient sut = new();

            byte[] receivedData = null;
            byte[] sentData = null;

            sut.MessageReceived = (data) => { receivedData = data; };
            sut.MessageSent = (data) => { sentData = data; };

            sut.Connect(processId);

            for (int i = 0; i < 100; i++)
            {
                if (receivedData != null && sentData != null)
                    break;

                System.Threading.Thread.Sleep(100);
            }

            sut.Disconnect();

            sut.Dispose();

            Assert.IsNotNull(receivedData);
            Assert.IsNotNull(sentData);
        }

        [TestMethod()]
        [TestCategory("Integration")]
        public void DeucalionClient_RepeatedInjectionTest()
        {
            // note: this requires starting FFXIV so it can be injected.
            int processId = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault()?.Id ?? 0;

            bool isValid = DeucalionInjector.ValidateLibraryChecksum();
            Assert.IsTrue(isValid);

            for (int i = 0; i < 100; i++)
            {
                bool injectionResult = DeucalionInjector.InjectLibrary(processId);
                Assert.IsTrue(injectionResult);

                DeucalionClient sut = new();

                byte[] receivedData = null;
                byte[] sentData = null;

                sut.MessageReceived = (data) => { receivedData = data; };
                sut.MessageSent = (data) => { sentData = data; };

                sut.Connect(processId);

                System.Threading.Thread.Sleep(500);

                sut.Disconnect();

                sut.Dispose();

                System.Threading.Thread.Sleep(1000);
            }

            Assert.IsTrue(true);
        }
    }
}

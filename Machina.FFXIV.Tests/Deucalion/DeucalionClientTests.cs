using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina.FFXIV.Deucalion;
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Machina.FFXIV.Deucalion.Tests
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

            string library = DeucalionInjector.ExtractLibrary();
            bool injectionResult = DeucalionInjector.InjectLibrary(processId, library);
            Assert.IsTrue(injectionResult);

            var sut = new DeucalionClient();

            byte[] receivedData = null;
            byte[] sentData = null;
            
            sut.MessageReceived = (byte[] data) => { receivedData = data; };
            sut.MessageSent = (byte[] data) => { sentData = data; };

            sut.Connect(processId);

            for (int i = 0; i < 100; i++)
            {
                if (receivedData != null && sentData != null)
                    break;

                System.Threading.Thread.Sleep(10);
            }

            sut.Disconnect();

            sut.Dispose();
            Assert.IsNotNull(receivedData);
            Assert.IsNotNull(sentData);
        }
    }
}

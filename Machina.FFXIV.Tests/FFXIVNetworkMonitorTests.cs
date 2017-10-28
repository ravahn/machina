using System;
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

            var sut = new FFXIVNetworkMonitor();
            sut.MessageReceived = (long epoch, byte[] data) =>
                { dataReceived = true; };
            sut.Start();
            for (int i=0;i<100;i++)
            {
                if (dataReceived == true)
                    break;
                System.Threading.Thread.Sleep(10);
            }
            sut.Stop();

            Assert.IsTrue(dataReceived);
        }
    }
}

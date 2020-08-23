using Microsoft.VisualStudio.TestTools.UnitTesting;
using Machina;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina.Tests
{
    [TestClass()]
    public class FirewallWrapperTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestInfrastructure.Listener.Messages.Clear();
        }

        [TestMethod()]
        public void FirewallWrapper_IsFirewallDisabledTest()
        {
            var sut = new FirewallWrapper();

            var result = sut.IsFirewallEnabled();

            // result could be either true or false based on local configuration.

            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod()]
        public void FirewallWrapper_IsFirewallApplicationConfiguredTest()
        {
            var sut = new FirewallWrapper();

            var result = sut.IsFirewallApplicationConfigured("Machina Test");

            Assert.IsTrue(result);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod()]
        public void FirewallWrapper_IsFirewallRuleConfiguredTest()
        {
            var sut = new FirewallWrapper();

            var result = sut.IsFirewallRuleConfigured("Machina Test");

            Assert.IsTrue(result);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }

        [TestMethod()]
        public void FirewallWrapper_AddFirewallApplicationEntryTest()
        {
            var sut = new FirewallWrapper();

            sut.AddFirewallApplicationEntry("Machina Test", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }


        [TestMethod()]
        public void FirewallWrapper_RemoveFirewallApplicationEntryTest()
        {
            var sut = new FirewallWrapper();

            sut.RemoveFirewallApplicationEntry("Machina Test");
            Assert.AreEqual(0, TestInfrastructure.Listener.Messages.Count);
        }
    }
}
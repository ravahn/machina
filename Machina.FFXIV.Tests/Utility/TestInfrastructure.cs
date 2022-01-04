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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Machina.FFXIV.Tests.Utility
{
    [TestClass]
    public class TestInfrastructure
    {
        public static MemoryTraceListener Listener = new MemoryTraceListener();

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _1)
        {
            // set up listener
            if (!Trace.Listeners.Contains(Listener))
                _ = Trace.Listeners.Add(Listener);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // set up listener
            if (Trace.Listeners.Contains(Listener))
                Trace.Listeners.Remove(Listener);
        }
    }
}

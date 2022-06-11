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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Runtime.InteropServices;
using Machina.FFXIV.Memory;

namespace Machina.FFXIV.Tests.Memory
{
    [TestClass()]
    public class SigScanTests
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [TestMethod()]
        public void ReadTest()
        {
            IntPtr _libraryHandle = LoadLibraryW(@"C:\Program Files (x86)\FINAL FANTASY XIV - A Realm Reborn\game\ffxiv_dx11.exe");

            SigScan sut = new();

            System.Collections.Generic.Dictionary<SignatureType, int> result = sut.Read(_libraryHandle);

            Assert.IsTrue(result.Any());
        }
    }
}

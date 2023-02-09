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

using Machina.FFXIV.Memory;
using Machina.FFXIV.Oodle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Machina.FFXIV.Tests.Oodle
{
    [TestClass()]
    public class OodleNative_FfxivTests
    {
        private OodleNative_Ffxiv _sut;

        [TestInitialize()]
        public void TestInitialize()
        {
            const string path = @"C:\Program Files (x86)\FINAL FANTASY XIV - A Realm Reborn\game\ffxiv_dx11.exe";
            _sut = new OodleNative_Ffxiv(new SigScan());
            _sut.Initialize(path);
        }


        [TestCleanup()]
        public void TestCleanup()
        {
            _sut.UnInitialize();
        }

        [TestMethod()]
        public void InitializeTest()
        {
            bool result = _sut.Initialized;

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void UnInitializeTest()
        {
            _sut.UnInitialize();

            bool result = _sut.Initialized;

            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void OodleNetwork1UDP_State_SizeTest()
        {
            int result = _sut.OodleNetwork1UDP_State_Size();

            Assert.IsTrue(result > 0);
        }


        [TestMethod()]
        public void OodleNetwork1TCP_State_SizeTest()
        {
            int result = _sut.OodleNetwork1TCP_State_Size();

            Assert.IsTrue(result > 0);
        }

        [TestMethod()]
        public void OodleNetwork1_Shared_SizeTest()
        {
            const int htbits = 0x13;
            int result = _sut.OodleNetwork1_Shared_Size(htbits);

            Assert.IsTrue(result > 0);
        }

        [TestMethod()]
        public void OodleNetwork1_Shared_SetWindowTest()
        {
            const int htbits = 0x13;
            int sharedSize = _sut.OodleNetwork1_Shared_Size(htbits);

            byte[] window = new byte[0x8000];
            byte[] shared = new byte[sharedSize];

            _sut.OodleNetwork1_Shared_SetWindow(shared, htbits, window, window.Length);

            Assert.IsTrue(shared[0] != 0);
        }

        [TestMethod()]
        public void OodleNetwork1UDP_TrainTest()
        {
            const int htbits = 0x13;
            int stateSize = _sut.OodleNetwork1UDP_State_Size();
            int sharedSize = _sut.OodleNetwork1_Shared_Size(htbits);

            byte[] window = new byte[0x8000];
            byte[] shared = new byte[sharedSize];
            byte[] state = new byte[stateSize];

            _sut.OodleNetwork1_Shared_SetWindow(shared, htbits, window, window.Length);
            _sut.OodleNetwork1UDP_Train(state, shared, IntPtr.Zero, IntPtr.Zero, 0);

            Assert.IsTrue(state[4] != 0);
        }

        [TestMethod()]
        public void OodleNetwork1TCP_TrainTest()
        {
            const int htbits = 0x13;
            int stateSize = _sut.OodleNetwork1TCP_State_Size();
            int sharedSize = _sut.OodleNetwork1_Shared_Size(htbits);

            byte[] window = new byte[0x8000];
            byte[] shared = new byte[sharedSize];
            byte[] state = new byte[stateSize];

            _sut.OodleNetwork1_Shared_SetWindow(shared, htbits, window, window.Length);
            _sut.OodleNetwork1TCP_Train(state, shared, IntPtr.Zero, IntPtr.Zero, 0);

            Assert.IsTrue(state[4] != 0);
        }

        [TestMethod()]
        public unsafe void OodleNetwork1UDP_DecodeTest()
        {
            const int htbits = 0x13;
            int stateSize = _sut.OodleNetwork1UDP_State_Size();
            int sharedSize = _sut.OodleNetwork1_Shared_Size(htbits);

            byte[] window = new byte[0x8000];
            byte[] shared = new byte[sharedSize];
            byte[] state = new byte[stateSize];

            _sut.OodleNetwork1_Shared_SetWindow(shared, htbits, window, window.Length);
            _sut.OodleNetwork1UDP_Train(state, shared, IntPtr.Zero, IntPtr.Zero, 0);

            byte[] source = new byte[255];
            byte[] uncompressed = new byte[255];
            for (byte i = 0; i < source.Length; i++) source[i] = i;
            byte[] compressed = new byte[255];
            bool result = _sut.OodleNetwork1UDP_Encode(state, shared, source, source.Length, compressed);
            Assert.IsTrue(result);
            fixed (byte* ptr = compressed)
            {
                result = _sut.OodleNetwork1UDP_Decode(state, shared, new IntPtr(ptr), compressed.Length, uncompressed, uncompressed.Length);
            }
            Assert.IsTrue(result);
            result = true;

            // compare each byte
            for (byte i = 0; i < source.Length; i++)
                if (source[i] != uncompressed[i])
                    result = false;

            Assert.IsTrue(result);
        }


        [TestMethod()]
        public unsafe void OodleNetwork1TCP_DecodeTest()
        {
            const int htbits = 0x13;
            int stateSize = _sut.OodleNetwork1TCP_State_Size();
            int sharedSize = _sut.OodleNetwork1_Shared_Size(htbits);

            byte[] window = new byte[0x8000];
            byte[] shared = new byte[sharedSize];
            byte[] state = new byte[stateSize];

            _sut.OodleNetwork1_Shared_SetWindow(shared, htbits, window, window.Length);
            _sut.OodleNetwork1TCP_Train(state, shared, IntPtr.Zero, IntPtr.Zero, 0);

            byte[] source = new byte[255];
            byte[] uncompressed = new byte[255];
            for (byte i = 0; i < source.Length; i++) source[i] = i;
            byte[] compressed = new byte[255];
            bool result = _sut.OodleNetwork1TCP_Encode(state, shared, source, source.Length, compressed);
            Assert.IsTrue(result);
            fixed (byte* ptr = compressed)
            {
                result = _sut.OodleNetwork1TCP_Decode(state, shared, new IntPtr(ptr), compressed.Length, uncompressed, uncompressed.Length);
            }
            Assert.IsTrue(result);
            result = true;

            // compare each byte
            for (byte i = 0; i < source.Length; i++)
                if (source[i] != uncompressed[i])
                    result = false;

            Assert.IsTrue(result);
        }

    }
}

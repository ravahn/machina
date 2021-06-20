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

using System.Buffers;

namespace Machina.Infrastructure
{
    public static class BufferCache
    {
        private static readonly int BUFFER_LENGTH = (1024 * 64) + 1;

        public static byte[] AllocateBuffer()
        {
            return ArrayPool<byte>.Shared.Rent(BUFFER_LENGTH);
        }

        public static void ReleaseBuffer(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

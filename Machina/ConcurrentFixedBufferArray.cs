using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machina
{
    public class Buffer
    {
        public byte[] Data;
        public int AllocatedSize;
    }

    public class NetworkBufferFactory
    {
        /// goal: fixed array of buffers
        ///  thread-safe
        ///  recv data into buffer
        ///  

        private ConcurrentQueue<Buffer> _freeBufferQueue = new ConcurrentQueue<Buffer>();
        private ConcurrentQueue<Buffer> _allocatedBufferQueue = new ConcurrentQueue<Buffer>();

        private int _bufferSize = 1024 * 128;

        public NetworkBufferFactory(int initialBufferCount, int bufferSize)
        {
            if (bufferSize > 0)
                _bufferSize = bufferSize;

            if (initialBufferCount > 0)
                for (int i = 0; i < initialBufferCount; i++)
                    _freeBufferQueue.Enqueue(CreateNewBuffer());
        }

        public Buffer GetNextFreeBuffer()
        {
            Buffer result;

            // attempt to pull from free buffer queue
            if (!_freeBufferQueue.IsEmpty)
                if (_freeBufferQueue.TryDequeue(out result) && result.AllocatedSize > 0)
                    return result;

            return CreateNewBuffer();
        }

        public Buffer GetNextAllocatedBuffer()
        {
            Buffer buffer;
            // attempt to pull from free buffer queue
            if (!_allocatedBufferQueue.IsEmpty)
                if (_allocatedBufferQueue.TryDequeue(out buffer) && buffer != null)
                    return buffer;

            return null;
        }

        public void AddFreeBuffer(Buffer buffer)
        {
            if (buffer != null)
            {
                buffer.AllocatedSize = 0;
                _freeBufferQueue.Enqueue(buffer);
            }
        }

        public void AddAllocatedBuffer(Buffer buffer)
        {
            if (buffer != null)
                _allocatedBufferQueue.Enqueue(buffer);
        }

        private Buffer CreateNewBuffer()
        {
            return new Buffer { Data = new byte[_bufferSize], AllocatedSize = 0 };
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machina
{
    /// <summary>
    /// This class manages a number of byte arrays in order to optimize memory usage for raw network socket reading.
    ///   By recycling the arrays when the data is done, it reduces the amount of garbage collection 
    ///   which reduces the interruptions in network data capture, and thus reduces the frequency of lost packets.
    /// </summary>
    public class NetworkBufferFactory
    {
        /// <summary>
        /// Helper class to encapsulate each array
        /// </summary>
        public class Buffer
        {
            public byte[] Data;
            public int AllocatedSize;
        }

        // Internal queues used to store the two types of buffers - allocated an free.
        private ConcurrentQueue<Buffer> _freeBufferQueue = new ConcurrentQueue<Buffer>();
        private ConcurrentQueue<Buffer> _allocatedBufferQueue = new ConcurrentQueue<Buffer>();

        // default buffer size
        private int _bufferSize = 1024 * 64+1; // maximum TCP packet size is just under 64kb, and winsock seems to limit receive calls to this size.

        /// <summary>
        /// constructor for the buffer factory
        /// </summary>
        /// <param name="initialBufferCount">Initial number of buffers to pre-allocate</param>
        /// <param name="bufferSize">size of each buffer, in case the default size is insufficient</param>
        public NetworkBufferFactory(int initialBufferCount, int bufferSize)
        {
            if (bufferSize > 0)
                _bufferSize = bufferSize;

            if (initialBufferCount > 0)
                for (int i = 0; i < initialBufferCount; i++)
                    _freeBufferQueue.Enqueue(CreateNewBuffer());
        }

        /// <summary>
        /// Returns the next unused buffer from the queue.  If none are available, returns a new buffer.
        /// </summary>
        public Buffer GetNextFreeBuffer()
        {
            // attempt to pull from free buffer queue
            if (!_freeBufferQueue.IsEmpty)
                if (_freeBufferQueue.TryDequeue(out Buffer result))
                    return result;

            return CreateNewBuffer();
        }

        /// <summary>
        /// Returns the next used buffer from the queue.  If none are available, returns NULL
        /// </summary>
        public Buffer GetNextAllocatedBuffer()
        {
            // attempt to pull from allocated buffer queue
            if (!_allocatedBufferQueue.IsEmpty)
                if (_allocatedBufferQueue.TryDequeue(out Buffer buffer))
                    return buffer;

            return null;
        }

        /// <summary>
        /// Adds a buffer back to the free queue, so it can be reused.
        /// </summary>
        public void AddFreeBuffer(Buffer buffer)
        {
            if (buffer != null)
            {
                buffer.AllocatedSize = 0;
                _freeBufferQueue.Enqueue(buffer);
            }
        }

        /// <summary>
        /// Adds a buffer containing data to the allocated queue.
        /// </summary>
        public void AddAllocatedBuffer(Buffer buffer)
        {
            if (buffer != null)
                _allocatedBufferQueue.Enqueue(buffer);
        }


        /// <summary>
        /// Creates a new buffer, including allocating the array.
        /// </summary>
        private Buffer CreateNewBuffer()
        {
            return new Buffer { Data = new byte[_bufferSize], AllocatedSize = 0 };
        }
    }
}

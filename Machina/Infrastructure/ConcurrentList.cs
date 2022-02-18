// Copyright ?2021 sandtechnology - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Machina.Infrastructure
{
    public sealed class ConcurrentList<T> : IList<T>
    {
        private bool disposedValue;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly IList<T> _list;

        public ConcurrentList(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            else
            {
                _list = list;
            }

        }
        public sealed class ConcurrentListEnumerator : IEnumerator<T>
        {
            private int _index = -1;
            private readonly ConcurrentList<T> _list;
            private T _current;

            internal ConcurrentListEnumerator(ConcurrentList<T> list)
            {
                if (list == null)
                {
                    throw new ArgumentNullException(nameof(list));
                }
                else
                {
                    _list = list;
                }

            }
            T IEnumerator<T>.Current => _current;

            object IEnumerator.Current => _current;

            void IDisposable.Dispose()
            {
            }

            bool IEnumerator.MoveNext()
            {
                if (++_index >= ((ICollection<T>)_list).Count)
                {
                    return false;
                }
                else
                {
                    _current = ((IList<T>)_list)[_index];
                    return true;
                }
            }

            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
        T IList<T>.this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list[index];
                }
                finally
                {
                    _lock.ExitReadLock();
                }

            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    _list[index] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        int ICollection<T>.Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _list.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }

            }
        }

        bool ICollection<T>.IsReadOnly => _list.IsReadOnly;

        void ICollection<T>.Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        void ICollection<T>.Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _list.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

        }

        bool ICollection<T>.Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _list.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _lock.EnterReadLock();
            try
            {
                _list.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ConcurrentListEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentListEnumerator(this);
        }

        int IList<T>.IndexOf(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _list.IndexOf(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        void IList<T>.Insert(int index, T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.Insert(index, item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        void IList<T>.RemoveAt(int index)
        {
            _lock.EnterWriteLock();
            try
            {
                _list.RemoveAt(index);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        ~ConcurrentList()
        {
            if (_lock != null)
            {
                _lock.Dispose();
            }
        }
    }
}

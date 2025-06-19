using System;
using System.Collections;
using System.Collections.Generic;

namespace Snm.DataStructures
{
    public class BufferList<T> : IReadOnlyList<T>
    {
        private readonly List<T> buffer;
        private int _customCount;
        int IReadOnlyCollection<T>.Count => _customCount;

        public BufferList()
        {
            buffer = new List<T>();
            _customCount = 0;
        }

        public void Add(T item)
        {
            if (_customCount < buffer.Count)
            {
                buffer[_customCount] = item;
            }
            else
            {
                buffer.Add(item);
            }
            _customCount++;
        }

        public void ResetCustomCount()
        {
            _customCount = 0;
        }

        public void ResetUnusedSlots()
        {
            for (var i = _customCount; i < buffer.Count; i++)
            {
                buffer[i] = default;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= _customCount)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is outside the custom count range.");
                return buffer[index];
            }
            set
            {
                if (index >= _customCount)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is outside the custom count range.");
                buffer[index] = value;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < _customCount; i++)
            {
                yield return buffer[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _customCount; i++)
            {
                yield return buffer[i];
            }
        }
    }
}
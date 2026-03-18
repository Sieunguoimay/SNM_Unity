using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public class StampBuffer
    {
        private readonly Vector4[] _buffer;
        private int _count;

        public int Count => _count;
        public int Capacity { get; }

        public StampBuffer(int capacity)
        {
            Capacity = capacity;
            _buffer = new Vector4[capacity];
        }

        public void Add(Vector4 stamp)
        {
            if (_count < Capacity)
                _buffer[_count++] = stamp;
        }

        public void Upload(Material material, int arrayId, int countId)
        {
            for (int i = _count; i < Capacity; i++)
                _buffer[i] = Vector4.zero;

            material.SetVectorArray(arrayId, _buffer);
            material.SetFloat(countId, _count);

            _count = 0;
        }
    }
}

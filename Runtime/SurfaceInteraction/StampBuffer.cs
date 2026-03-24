using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public class StampBuffer
    {
        private readonly Vector4[] _stamps;
        private int _count;

        public int Count => _count;
        public int Capacity { get; }

        public StampBuffer(int capacity)
        {
            Capacity = capacity;
            _stamps = new Vector4[capacity];
        }

        public void Add(Vector4 stamp)
        {
            if (_count >= Capacity)
            {
                Debug.LogWarning($"[StampBuffer] Buffer full ({Capacity}). Stamp dropped.");
                return;
            }
            _stamps[_count] = stamp;
            _count++;
        }

        public void Upload(Material material, int arrayId, int countId)
        {
            Pad();
            material.SetVectorArray(arrayId, _stamps);
            material.SetFloat(countId, _count);
        }

        public void UploadTo(Material material, int arrayId, int countId)
        {
            Pad();
            material.SetVectorArray(arrayId, _stamps);
            material.SetFloat(countId, _count);
        }

        public void Clear()
        {
            _count = 0;
        }

        void Pad()
        {
            for (int i = _count; i < Capacity; i++)
                _stamps[i] = Vector4.zero;
        }
    }
}

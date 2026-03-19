using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.SurfaceInteraction
{
    public class StampTextureBuffer : IDisposable
    {
        private readonly Vector4[] _stamps;
        private Texture2D _texture;
        private int _count;

        public int Count => _count;
        public int Capacity { get; }

        public StampTextureBuffer(int capacity)
        {
            Capacity = capacity;
            _stamps = new Vector4[capacity];
        }

        public void Add(Vector4 stamp)
        {
            if (_count >= Capacity)
            {
                Debug.LogWarning($"[StampTextureBuffer] Buffer full ({Capacity}). Stamp dropped.");
                return;
            }
            _stamps[_count] = stamp;
            _count++;
        }

        public void Upload(Material material, int texId, int countId)
        {
            EnsureTexture();

            var raw = _texture.GetRawTextureData<Vector4>();

            for (int i = 0; i < _count; i++)
                raw[i] = _stamps[i];
            for (int i = _count; i < raw.Length; i++)
                raw[i] = Vector4.zero;

            _texture.Apply(false, false);

            material.SetTexture(texId, _texture);
            material.SetFloat(countId, _count);
            _count = 0;
        }

        private void EnsureTexture()
        {
            if (_texture != null) return;
            _texture = new Texture2D(
                Capacity, 1,
                GraphicsFormat.R32G32B32A32_SFloat,
                TextureCreationFlags.None)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        public void Dispose()
        {
            if (_texture != null)
            {
                UnityEngine.Object.Destroy(_texture);
                _texture = null;
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// CPU→GPU bridge for per-frame disturbances. Stamps collect during the
    /// frame and upload as an Nx1 float texture (uv.x, uv.y, radius, strength
    /// per texel) right before the simulation pass.
    ///
    /// Fixes over V1's StampTextureBuffer: capacity is configurable, only the
    /// dirty texel range is rewritten (V1 zeroed the whole array every
    /// upload), and overflow logs one aggregate warning per frame instead of
    /// one per dropped stamp.
    /// </summary>
    public class StampBuffer : IDisposable
    {
        private readonly Vector4[] _stamps;
        private Texture2D _texture;
        private int _count;
        private int _uploadedCount;
        private int _droppedThisFrame;

        public int Count => _count;
        public int Capacity { get; }

        /// <summary>Stamps accepted in the most recent frame (for the stats panel).</summary>
        public int LastFrameCount { get; private set; }

        public StampBuffer(int capacity)
        {
            Capacity = Mathf.Max(1, capacity);
            _stamps = new Vector4[Capacity];
        }

        public void Add(Vector2 uv, float uvRadius, float strength)
        {
            if (_count >= Capacity)
            {
                _droppedThisFrame++;
                return;
            }
            _stamps[_count] = new Vector4(uv.x, uv.y, uvRadius, strength);
            _count++;
        }

        /// <summary>Uploads pending stamps and resets for the next frame.</summary>
        public void Upload(Material material, int texId, int countId)
        {
            if (_droppedThisFrame > 0)
            {
                Debug.LogWarning($"[WaterSystemV2] Stamp buffer full ({Capacity}); dropped {_droppedThisFrame} stamps this frame. Raise Wave Settings → Stamp Capacity.");
                _droppedThisFrame = 0;
            }

            EnsureTexture();

            var raw = _texture.GetRawTextureData<Vector4>();

            for (int i = 0; i < _count; i++)
                raw[i] = _stamps[i];

            // Clear only the texels the previous upload used beyond this one.
            for (int i = _count; i < _uploadedCount; i++)
                raw[i] = Vector4.zero;

            _texture.Apply(false, false);

            material.SetTexture(texId, _texture);
            material.SetFloat(countId, _count);

            _uploadedCount = _count;
            LastFrameCount = _count;
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
                name = "WaterStampBuffer",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        public void Dispose()
        {
            WaterObjects.Destroy(_texture);
            _texture = null;
        }
    }
}

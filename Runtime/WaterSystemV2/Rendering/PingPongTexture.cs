using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>Two render textures swapped as source/target each blit.</summary>
    public class PingPongTexture : IDisposable
    {
        public RenderTexture A { get; }
        public RenderTexture B { get; }

        private bool _toggle;

        public PingPongTexture(RenderTextureDescriptor desc)
        {
            A = Create(desc);
            B = Create(desc);
            Clear();
        }

        public RenderTexture Current => _toggle ? A : B;

        public (RenderTexture src, RenderTexture dst) GetPair()
        {
            var src = _toggle ? A : B;
            var dst = _toggle ? B : A;
            _toggle = !_toggle;
            return (src, dst);
        }

        public void Clear()
        {
            ClearRT(A);
            ClearRT(B);
            _toggle = false;
        }

        public void Dispose()
        {
            Release(A);
            Release(B);
        }

        private static RenderTexture Create(RenderTextureDescriptor desc)
        {
            var rt = new RenderTexture(desc)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
            };
            rt.Create();
            return rt;
        }

        private static void ClearRT(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
        }

        private static void Release(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            WaterObjects.Destroy(rt);
        }
    }
}

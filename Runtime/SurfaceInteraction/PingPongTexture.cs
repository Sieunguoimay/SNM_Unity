using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public class PingPongTexture : IDisposable
    {
        public RenderTexture A { get; }
        public RenderTexture B { get; }

        private bool toggle;

        public PingPongTexture(RenderTextureDescriptor desc)
        {
            A = new RenderTexture(desc);
            B = new RenderTexture(desc);

            A.filterMode = FilterMode.Bilinear;
            A.wrapMode = TextureWrapMode.Clamp;
            A.useMipMap = false;
            A.autoGenerateMips = false;

            B.filterMode = FilterMode.Bilinear;
            B.wrapMode = TextureWrapMode.Clamp;
            B.useMipMap = false;
            B.autoGenerateMips = false;

            A.Create();
            B.Create();

            Clear();
        }

        public (RenderTexture src, RenderTexture dst) GetPair()
        {
            var src = toggle ? A : B;
            var dst = toggle ? B : A;

            toggle = !toggle;

            return (src, dst);
        }

        public RenderTexture Current => toggle ? B : A;

        public void Clear()
        {
            ClearRT(A);
            ClearRT(B);
            toggle = false;
        }

        public void Dispose()
        {
            A.Release();
            B.Release();

            UnityEngineUtility.DestroyObject(A);
            UnityEngineUtility.DestroyObject(B);
        }

        private static void ClearRT(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
        }
    }
}

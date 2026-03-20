using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public class SurfaceStampRenderer : IDisposable
    {
        private readonly PingPongTexture _pingPong;
        private readonly Material _material;

        public Material Material => _material;
        public RenderTexture ResultTexture => _pingPong.Current;

        public SurfaceStampRenderer(Material material, PingPongTexture pingPong)
        {
            _material = material;
            _pingPong = pingPong;
        }

        private static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");

        public void Render(int iterations = 1)
        {
            for (int i = 0; i < iterations; i++)
            {
                var (src, dst) = _pingPong.GetPair();
                _material.SetTexture(ID_MainTex, src);
                Graphics.Blit(src, dst, _material);
            }
        }

        public void Clear() => _pingPong.Clear();

        public void Dispose()
        {
            if (_material != null)
                UnityEngineUtility.DestroyObject(_material);

            _pingPong?.Dispose();
        }
    }
}

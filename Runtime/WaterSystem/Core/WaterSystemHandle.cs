using System;
using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemHandle : IDisposable
    {
        private readonly IDisposable dispose;

        public RenderTexture ReflectionTexture { get; }
        public RenderTexture WaveDisplayTexture { get; }

        public WaterSystemHandle(
            IDisposable dispose,
            RenderTexture reflectionTexture = null,
            RenderTexture waveDisplayTexture = null)
        {
            this.dispose = dispose;
            ReflectionTexture = reflectionTexture;
            WaveDisplayTexture = waveDisplayTexture;
        }

        public void Dispose()
        {
            dispose.Dispose();
        }
    }
}
using System;
using Snm.WaterSystem.Wave;
using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemHandle : IDisposable
    {
        private readonly IDisposable dispose;

        public RenderTexture ReflectionTexture { get; }
        public IWaveSimulation WaveSimulation { get; }

        public WaterSystemHandle(
            IDisposable dispose,
            RenderTexture reflectionTexture = null,
            IWaveSimulation waveSimulation = null)
        {
            this.dispose = dispose;
            ReflectionTexture = reflectionTexture;
            WaveSimulation = waveSimulation;
        }

        public void Dispose()
        {
            dispose.Dispose();
        }
    }
}
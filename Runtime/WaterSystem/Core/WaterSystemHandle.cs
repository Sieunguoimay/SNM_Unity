using System;
using Snm.WaterSystem.Wave;
using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemHandle : IDisposable
    {
        private readonly IDisposable _scope;

        public RenderTexture ReflectionTexture { get; }
        public IWaveSimulation WaveSimulation { get; }

        public WaterSystemHandle(
            IDisposable scope,
            RenderTexture reflectionTexture = null,
            IWaveSimulation waveSimulation = null)
        {
            _scope = scope;
            ReflectionTexture = reflectionTexture;
            WaveSimulation = waveSimulation;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
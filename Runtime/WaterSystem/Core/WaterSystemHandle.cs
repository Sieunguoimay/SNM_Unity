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
        public WaveDisturberTracker DisturberTracker { get; }

        public WaterSystemHandle(
            IDisposable scope,
            RenderTexture reflectionTexture = null,
            IWaveSimulation waveSimulation = null,
            WaveDisturberTracker disturberTracker = null)
        {
            _scope = scope;
            ReflectionTexture = reflectionTexture;
            WaveSimulation = waveSimulation;
            DisturberTracker = disturberTracker;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
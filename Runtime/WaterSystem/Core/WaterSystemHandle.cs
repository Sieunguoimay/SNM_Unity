using System;
using Snm.WaterSystem.Buoyancy;
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
        public BuoyancyTracker BuoyancyTracker { get; }

        public WaterSystemHandle(
            IDisposable scope,
            RenderTexture reflectionTexture = null,
            IWaveSimulation waveSimulation = null,
            WaveDisturberTracker disturberTracker = null,
            BuoyancyTracker buoyancyTracker = null)
        {
            _scope = scope;
            ReflectionTexture = reflectionTexture;
            WaveSimulation = waveSimulation;
            DisturberTracker = disturberTracker;
            BuoyancyTracker = buoyancyTracker;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
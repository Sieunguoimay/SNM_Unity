using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveSimulationPass : IDisposable
    {
        void Execute(WaveSimulationConfig config);
        RenderTexture GetResult();
        void Clear();
    }
}

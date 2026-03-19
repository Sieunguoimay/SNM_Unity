using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveSimulation : IDisposable
    {
        void AddDisturbance(WaveDisturbance disturbance);
        RenderTexture GetDisplayTexture();
        RenderTexture GetSimulationTexture();
        void ClearSimulation();
        WaveConfig Config { get; }
    }
}

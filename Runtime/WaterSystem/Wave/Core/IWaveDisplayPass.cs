using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveDisplayPass : IDisposable
    {
        void Render(
            RenderTexture waveTexture,
            RenderTexture heightfield,
            RenderTexture target,
            float heightfieldStrength,
            int displayMode);
    }
}

using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationPass : IWaveSimulationPass
    {
        private readonly SurfaceStampRenderer _renderer;
        private readonly StampBuffer _stampBuffer;

        private readonly int ID_Damping = Shader.PropertyToID("_Damping");
        private readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private readonly int ID_Disturbances = Shader.PropertyToID("_Disturbances");
        private readonly int ID_DisturbanceCount = Shader.PropertyToID("_DisturbanceCount");

        public WaveSimulationPass(SurfaceStampRenderer renderer, StampBuffer stampBuffer)
        {
            _renderer = renderer;
            _stampBuffer = stampBuffer;
        }

        public void Execute(WaveSimulationConfig config)
        {
            var mat = _renderer.Material;

            int steps = Mathf.Max(1, config.iterationsPerFrame);

            // Damping is authored as per-frame. Convert to per-iteration
            // so the visual decay rate stays the same regardless of step count.
            float perIterationDamping = Mathf.Pow(config.damping, 1f / steps);
            mat.SetFloat(ID_Damping, perIterationDamping);
            mat.SetFloat(ID_WaveSpeed, Mathf.Min(config.waveSpeed, 0.5f));

            if (_stampBuffer.Count > 0)
            {
                _stampBuffer.Upload(mat, ID_Disturbances, ID_DisturbanceCount);
                _renderer.Render(1);

                if (steps > 1)
                {
                    mat.SetFloat(ID_DisturbanceCount, 0);
                    _renderer.Render(steps - 1);
                }
            }
            else
            {
                mat.SetFloat(ID_DisturbanceCount, 0);
                _renderer.Render(steps);
            }
        }

        public RenderTexture GetResult() => _renderer.ResultTexture;

        public void Clear() => _renderer.Clear();

        public void Dispose() => _renderer.Dispose();
    }
}

using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationPass : IWaveSimulationPass
    {
        private readonly SurfaceStampRenderer _renderer;
        private readonly StampTextureBuffer _stampBuffer;

        private int _frameCounter;

        private readonly int ID_Damping = Shader.PropertyToID("_Damping");
        private readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private readonly int ID_StampTex = Shader.PropertyToID("_StampTex");
        private readonly int ID_StampCount = Shader.PropertyToID("_StampCount");

        private readonly int ID_RainEnabled = Shader.PropertyToID("_RainEnabled");
        private readonly int ID_RainIntensity = Shader.PropertyToID("_RainIntensity");
        private readonly int ID_RainDensity = Shader.PropertyToID("_RainDensity");
        private readonly int ID_RainFrame = Shader.PropertyToID("_RainFrame");

        public WaveSimulationPass(SurfaceStampRenderer renderer, StampTextureBuffer stampBuffer)
        {
            _renderer = renderer;
            _stampBuffer = stampBuffer;
        }

        public void Execute(WaveConfig config)
        {
            var mat = _renderer.Material;

            int steps = Mathf.Max(1, config.iterationsPerFrame);

            float perIterationDamping = Mathf.Pow(config.damping, 1f / steps);
            mat.SetFloat(ID_Damping, perIterationDamping);
            mat.SetFloat(ID_WaveSpeed, Mathf.Min(config.waveSpeed, 0.5f));

            SetRainUniforms(mat, config);

            // First iteration: stamps + rain enabled
            if (_stampBuffer.Count > 0)
                _stampBuffer.Upload(mat, ID_StampTex, ID_StampCount);
            else
                mat.SetFloat(ID_StampCount, 0);

            _renderer.Render(1);

            // Remaining iterations: propagation only (no stamps, no rain)
            if (steps > 1)
            {
                mat.SetFloat(ID_StampCount, 0);
                mat.SetFloat(ID_RainEnabled, 0);
                _renderer.Render(steps - 1);
            }

            _frameCounter++;
        }

        private void SetRainUniforms(Material mat, WaveConfig config)
        {
            var rain = config.rain;
            if (rain == null || !rain.enabled)
            {
                mat.SetFloat(ID_RainEnabled, 0);
                return;
            }

            mat.SetFloat(ID_RainEnabled, 1);
            mat.SetFloat(ID_RainIntensity, rain.intensity);
            mat.SetFloat(ID_RainFrame, _frameCounter);

            // Convert "drops per second" to probability per pixel.
            float texSize = _renderer.ResultTexture.width;
            float totalPixels = texSize * texSize;
            float dropsPerFrame = rain.density * Time.deltaTime;
            float probability = Mathf.Clamp01(dropsPerFrame / totalPixels);
            mat.SetFloat(ID_RainDensity, probability);
        }

        public RenderTexture GetResult() => _renderer.ResultTexture;

        public void Clear() => _renderer.Clear();

        public void Dispose()
        {
            _stampBuffer.Dispose();
            _renderer.Dispose();
        }
    }
}

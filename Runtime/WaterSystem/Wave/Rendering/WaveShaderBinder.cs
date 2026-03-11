using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveShaderBinder : IUpdateTarget
    {
        private static readonly int WaveTexID = Shader.PropertyToID("_WaveTex");
        private static readonly int WaveNormalStrengthID = Shader.PropertyToID("_WaveNormalStrength");

        private readonly Material _surfaceMaterial;
        private readonly IWaveSimulation _simulation;

        public WaveShaderBinder(Material surfaceMaterial, IWaveSimulation simulation)
        {
            _surfaceMaterial = surfaceMaterial;
            _simulation = simulation;
        }

        public void Update(float deltaTime)
        {
            _surfaceMaterial.SetTexture(WaveTexID, _simulation.GetSimulationTexture());
            _surfaceMaterial.SetFloat(WaveNormalStrengthID, _simulation.Config.waveNormalStrength);
        }

        public void Dispose() { }
    }
}

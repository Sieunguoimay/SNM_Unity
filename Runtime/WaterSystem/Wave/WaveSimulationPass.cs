using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationPass : IWaveSimulationPass
    {
        private readonly Material material;
        private readonly PingPongTexture pingPong;
        private readonly DisturbanceBuffer disturbanceBuffer;

        private readonly int ID_Damping = Shader.PropertyToID("_Damping");
        private readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private readonly int ID_Disturbances = Shader.PropertyToID("_Disturbances");
        private readonly int ID_DisturbanceCount = Shader.PropertyToID("_DisturbanceCount");

        public WaveSimulationPass(
            Material material,
            PingPongTexture pingPong,
            DisturbanceBuffer disturbanceBuffer)
        {
            this.material = material;
            this.pingPong = pingPong;
            this.disturbanceBuffer = disturbanceBuffer;
        }

        public void Execute(WaveSimulationConfig config)
        {
            material.SetFloat(ID_Damping, config.damping);

            float stableWaveSpeed = Mathf.Min(config.waveSpeed, 0.5f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(config.waveSpreadSpeed));
            if (config.maxIterations > 0)
                steps = Mathf.Min(steps, config.maxIterations);

            disturbanceBuffer.Upload(material, ID_Disturbances, ID_DisturbanceCount);

            for (int i = 0; i < steps; i++)
            {
                material.SetFloat(ID_WaveSpeed, stableWaveSpeed);

                var (src, dst) = pingPong.GetPair();
                Graphics.Blit(src, dst, material);
            }
        }

        public RenderTexture GetResult() => pingPong.Current;

        public void Clear() => pingPong.Clear();

        public void Dispose()
        {
            if (material != null)
                UnityEngineUtility.DestroyObject(material);

            pingPong?.Dispose();
        }
    }
}

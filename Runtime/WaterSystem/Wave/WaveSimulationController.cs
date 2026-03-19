using System;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationController : IWaveSimulation, IWaterFeature
    {
        private static readonly int WaveTexID = Shader.PropertyToID("_WaveTex");
        private static readonly int WaveNormalStrengthID = Shader.PropertyToID("_WaveNormalStrength");

        private readonly IWaveSimulationPass simulation;
        private readonly IWaveDisplayPass display;
        private readonly StampTextureBuffer stampBuffer;
        private readonly RenderTexture displayTexture;
        private readonly Material surfaceMaterial;

        public WaveConfig Config { get; }

        public WaveSimulationController(
            IWaveSimulationPass simulation,
            IWaveDisplayPass display,
            StampTextureBuffer stampBuffer,
            RenderTexture displayTexture,
            WaveConfig config,
            Material surfaceMaterial = null)
        {
            this.simulation = simulation;
            this.display = display;
            this.stampBuffer = stampBuffer;
            this.displayTexture = displayTexture;
            Config = config;
            this.surfaceMaterial = surfaceMaterial;
        }

        public void OnUpdate(float deltaTime)
        {
            simulation.Execute(Config);

            display.Render(
                simulation.GetResult(),
                simulation.GetResult(),
                displayTexture,
                Config.heightfieldStrength,
                Config.displayMode);

            if (surfaceMaterial != null)
            {
                surfaceMaterial.SetTexture(WaveTexID, simulation.GetResult());
                surfaceMaterial.SetFloat(WaveNormalStrengthID, Config.waveNormalStrength);
            }
        }

        public void AddDisturbance(WaveDisturbance disturbance)
        {
            stampBuffer.Add(new Vector4(
                disturbance.uvPos.x,
                disturbance.uvPos.y,
                disturbance.radius,
                disturbance.strength));
        }

        public RenderTexture GetDisplayTexture() => displayTexture;

        public RenderTexture GetSimulationTexture() => simulation.GetResult();

        public void ClearSimulation()
        {
            simulation.Clear();
        }

        public void Dispose()
        {
            if (displayTexture != null)
            {
                displayTexture.Release();
                UnityEngineUtility.DestroyObject(displayTexture);
            }
        }
    }
}

using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationController : IWaveSimulation, IWaterFeature
    {
        private static readonly int WaveTexID = Shader.PropertyToID("_WaveTex");
        private static readonly int WaveNormalStrengthID = Shader.PropertyToID("_WaveNormalStrength");

        private readonly IWaveSimulationPass simulation;
        private readonly IWaveDisplayPass display;
        private readonly DisturbanceBuffer disturbances;
        private readonly RenderTexture displayTexture;
        private readonly Material surfaceMaterial;

        public WaveSimulationConfig Config { get; }

        public WaveSimulationController(
            IWaveSimulationPass simulation,
            IWaveDisplayPass display,
            DisturbanceBuffer disturbances,
            RenderTexture displayTexture,
            WaveSimulationConfig config,
            Material surfaceMaterial = null)
        {
            this.simulation = simulation;
            this.display = display;
            this.disturbances = disturbances;
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
            disturbances.Add(disturbance);
        }

        public RenderTexture GetDisplayTexture() => displayTexture;

        public RenderTexture GetSimulationTexture() => simulation.GetResult();

        public void ClearSimulation()
        {
            simulation.Clear();
        }

        public void Dispose()
        {
            // simulation.Dispose();
            // display.Dispose();

            if (displayTexture != null)
            {
                displayTexture.Release();
                UnityEngineUtility.DestroyObject(displayTexture);
            }
        }
    }
}

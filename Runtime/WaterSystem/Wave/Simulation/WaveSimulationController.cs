using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationController : IWaveSimulation, IUpdateTarget
    {
        private readonly IWaveSimulationPass simulation;
        private readonly IWaveDisplayPass display;
        private readonly DisturbanceBuffer disturbances;
        private readonly RenderTexture displayTexture;
        private readonly IUpdateService updateService;

        public WaveSimulationConfig Config { get; }

        public WaveSimulationController(
            IWaveSimulationPass simulation,
            IWaveDisplayPass display,
            DisturbanceBuffer disturbances,
            RenderTexture displayTexture,
            WaveSimulationConfig config,
            IUpdateService updateService)
        {
            this.simulation = simulation;
            this.display = display;
            this.disturbances = disturbances;
            this.displayTexture = displayTexture;
            Config = config;
            this.updateService = updateService;

            updateService.AddUpdateTarget(this);
        }

        public void Update(float deltaTime)
        {
            simulation.Execute(Config);

            display.Render(
                simulation.GetResult(),
                simulation.GetResult(),
                displayTexture,
                Config.heightfieldStrength,
                Config.displayMode);
        }

        public void AddDisturbance(WaveDisturbance disturbance)
        {
            disturbances.Add(disturbance);
        }

        public RenderTexture GetDisplayTexture() => displayTexture;

        public void ClearSimulation()
        {
            simulation.Clear();
        }

        public void Dispose()
        {
            updateService.RemoveUpdateTarget(this);

            if (displayTexture != null)
            {
                displayTexture.Release();
                UnityEngineUtility.DestroyObject(displayTexture);
            }
        }
    }
}

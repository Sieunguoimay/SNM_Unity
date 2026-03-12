using Snm.WaterSystem.Wave;
using UnityEngine;

namespace Snm.WaterSystem.Rain
{
    public class RainFeature : IWaterFeature
    {
        private readonly RainConfig _config;
        private readonly IWaveSimulation _waveSimulation;
        private float _dropAccumulator;

        private const int MaxDropsPerFrame = 16;

        public RainFeature(IWaveSimulation waveSimulation, RainConfig config)
        {
            _waveSimulation = waveSimulation;
            _config = config;
        }

        public void OnUpdate(float deltaTime)
        {
            _dropAccumulator += _config.density * deltaTime;

            int drops = Mathf.Min(Mathf.FloorToInt(_dropAccumulator), MaxDropsPerFrame);
            _dropAccumulator -= drops;

            for (int i = 0; i < drops; i++)
            {
                var disturbance = new WaveDisturbance
                {
                    uvPos = new Vector2(Random.value, Random.value),
                    radius = _config.dropRadius,
                    strength = _config.intensity
                };
                _waveSimulation.AddDisturbance(disturbance);
            }
        }

        public void Dispose() { }
    }
}

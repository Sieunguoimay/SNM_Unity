using UnityEngine;

namespace Snm.WaterSystem.Shoreline
{
    public class ShorelineFeature : IWaterFeature
    {
        private readonly ShorelineConfig _config;
        private readonly ShorelineShaderBinder _binder;

        public ShorelineFeature(Material material, ShorelineConfig config)
        {
            _config = config;
            _binder = new ShorelineShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.waveCount,
                _config.speed,
                _config.foamStrength,
                _config.foamScale);
        }

        public void Dispose() { }
    }
}

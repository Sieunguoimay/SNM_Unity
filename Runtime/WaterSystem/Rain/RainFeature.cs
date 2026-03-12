using UnityEngine;

namespace Snm.WaterSystem.Rain
{
    public class RainFeature : IWaterFeature
    {
        private readonly RainConfig _config;
        private readonly RainShaderBinder _binder;

        public RainFeature(Material material, RainConfig config)
        {
            _config = config;
            _binder = new RainShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.rippleTexture,
                _config.intensity,
                _config.density,
                _config.speed,
                _config.scale);
        }

        public void Dispose() { }
    }
}

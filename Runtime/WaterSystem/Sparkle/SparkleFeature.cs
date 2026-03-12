using UnityEngine;

namespace Snm.WaterSystem.Sparkle
{
    public class SparkleFeature : IWaterFeature
    {
        private readonly SparkleConfig _config;
        private readonly SparkleShaderBinder _binder;

        public SparkleFeature(Material material, SparkleConfig config)
        {
            _config = config;
            _binder = new SparkleShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.intensity,
                _config.density,
                _config.speed);
        }

        public void Dispose() { }
    }
}

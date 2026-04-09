using UnityEngine;

namespace Snm.WaterSystem.IntersectionBands
{
    public class IntersectionBandsFeature : IWaterFeature
    {
        private readonly IntersectionBandsConfig _config;
        private readonly IntersectionBandsShaderBinder _binder;

        public IntersectionBandsFeature(Material material, IntersectionBandsConfig config)
        {
            _config = config;
            _binder = new IntersectionBandsShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.lineCount,
                _config.speed,
                _config.strength,
                _config.sharpness,
                _config.maxDepth);
        }

        public void Dispose() { }
    }
}

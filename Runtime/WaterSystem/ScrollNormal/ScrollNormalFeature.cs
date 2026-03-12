using UnityEngine;

namespace Snm.WaterSystem.ScrollNormal
{
    public class ScrollNormalFeature : IWaterFeature
    {
        private readonly ScrollNormalConfig _config;
        private readonly ScrollNormalShaderBinder _binder;

        public ScrollNormalFeature(Material material, ScrollNormalConfig config)
        {
            _config = config;
            _binder = new ScrollNormalShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.normalMap,
                _config.strength,
                _config.scale,
                _config.speed1,
                _config.speed2);
        }

        public void Dispose() { }
    }
}

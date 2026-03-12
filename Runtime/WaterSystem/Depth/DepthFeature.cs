using System;
using UnityEngine;

namespace Snm.WaterSystem.Depth
{
    public class DepthFeature : IWaterFeature
    {
        private readonly DepthConfig _config;
        private readonly DepthShaderBinder _binder;

        public DepthFeature(
            Material material,
            DepthConfig config)
        {
            _config = config;
            _binder = new DepthShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.shallowColor,
                _config.deepColor,
                _config.absorption);
        }

        public void Dispose() { }
    }
}

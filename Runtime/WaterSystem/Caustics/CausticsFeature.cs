using System;
using UnityEngine;

namespace Snm.WaterSystem.Caustics
{
    public class CausticsFeature : IWaterFeature
    {
        private readonly CausticsConfig _config;
        private readonly CausticsShaderBinder _binder;

        public CausticsFeature(
            Material material,
            CausticsConfig config)
        {
            _config = config;
            _binder = new CausticsShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.causticsTexture,
                _config.strength,
                _config.scale,
                _config.speed,
                _config.split);
        }

        public void Dispose() { }
    }
}

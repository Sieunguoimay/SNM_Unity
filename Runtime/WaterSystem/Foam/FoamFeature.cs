using UnityEngine;

namespace Snm.WaterSystem.Foam
{
    public class FoamFeature : IWaterFeature
    {
        private readonly FoamConfig _config;
        private readonly FoamShaderBinder _binder;

        public FoamFeature(Material material, FoamConfig config)
        {
            _config = config;
            _binder = new FoamShaderBinder(material);
        }

        public void OnUpdate(float deltaTime)
        {
            _binder.Bind(
                _config.foamTexture,
                _config.strength,
                _config.depthThreshold,
                _config.scale,
                _config.speed);
        }

        public void Dispose() { }
    }
}

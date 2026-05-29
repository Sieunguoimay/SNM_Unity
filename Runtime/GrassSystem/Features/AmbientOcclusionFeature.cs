using UnityEngine;

namespace Snm.GrassSystem
{
    public class AmbientOcclusionFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        bool _applied;
        float _lastStrength;
        float _lastPower;

        public AmbientOcclusionFeature(GrassFeatureContext ctx)
        {
            _ctx = ctx;
            BindIfDirty();
        }

        public void OnUpdate(float deltaTime)
        {
            BindIfDirty();
        }

        public void Dispose() { }

        void BindIfDirty()
        {
            var config = _ctx.Config.ambientOcclusion;
            if (_applied && _lastStrength == config.strength && _lastPower == config.power)
                return;

            _lastStrength = config.strength;
            _lastPower = config.power;
            _applied = true;

            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetFloat(ShaderIDs.AOStrength, config.strength);
                mat.SetFloat(ShaderIDs.AOPower, config.power);
            }
        }

        static class ShaderIDs
        {
            public static readonly int AOStrength = Shader.PropertyToID("_AOStrength");
            public static readonly int AOPower = Shader.PropertyToID("_AOPower");
        }
    }
}

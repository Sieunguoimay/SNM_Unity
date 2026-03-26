using UnityEngine;

namespace Snm.GrassSystem
{
    public class AmbientOcclusionFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        public AmbientOcclusionFeature(GrassFeatureContext ctx)
        {
            _ctx = ctx;
            BindConfig();
        }

        public void OnUpdate(float deltaTime)
        {
            BindConfig();
        }

        public void Dispose() { }

        void BindConfig()
        {
            var config = _ctx.Config.ambientOcclusion;
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

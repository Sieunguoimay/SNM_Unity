using UnityEngine;

namespace Snm.GrassSystem
{
    public class ColorVariationFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        public ColorVariationFeature(GrassFeatureContext ctx)
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
            var config = _ctx.Config.colorVariation;
            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetColor(ShaderIDs.ColorVariationA, config.colorA);
                mat.SetColor(ShaderIDs.ColorVariationB, config.colorB);
            }
        }

        static class ShaderIDs
        {
            public static readonly int ColorVariationA = Shader.PropertyToID("_ColorVariationA");
            public static readonly int ColorVariationB = Shader.PropertyToID("_ColorVariationB");
        }
    }
}

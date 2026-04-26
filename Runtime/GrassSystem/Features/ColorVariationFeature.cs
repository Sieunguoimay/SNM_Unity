using UnityEngine;

namespace Snm.GrassSystem
{
    public class ColorVariationFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        bool _applied;
        Color _lastColorA;
        Color _lastColorB;

        public ColorVariationFeature(GrassFeatureContext ctx)
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
            var config = _ctx.Config.colorVariation;
            if (_applied && _lastColorA == config.colorA && _lastColorB == config.colorB)
                return;

            _lastColorA = config.colorA;
            _lastColorB = config.colorB;
            _applied = true;

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

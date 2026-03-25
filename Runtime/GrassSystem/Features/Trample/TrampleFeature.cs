using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class TrampleFeature : IGrassFeature
    {
        private readonly GrassTrample _trample;
        private readonly GrassFeatureContext _ctx;

        public GrassTrample Trample => _trample;

        public TrampleFeature(
            GrassFeatureContext ctx)
        {
            _ctx = ctx;
            _trample = new GrassTrample();
            _trample.Setup(ctx.Config, ctx.Canvas);

            foreach (var mat in ctx.AllMaterials)
                mat.SetTexture(ShaderIDs.TrampleMap, _trample.OutputTexture);
        }

        public void OnUpdate(float deltaTime)
        {
            _trample.ApplyConfig(_ctx.Config.trample, _ctx.Config.interactionHeight);
            _trample.Update(deltaTime);
            _trample.ClearBrushes();
        }

        public void Dispose()
        {
            _trample.Dispose();
        }

        static class ShaderIDs
        {
            public static readonly int TrampleMap = Shader.PropertyToID("_TrampleMap");
        }
    }
}

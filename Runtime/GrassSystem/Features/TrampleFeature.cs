using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class TrampleFeature : IGrassFeature
    {
        private readonly GrassTrample _trample;
        private readonly Material _grassMaterial;

        public GrassTrample Trample => _trample;

        public TrampleFeature(
            GrassFeatureContext ctx,
            Material grassMaterial)
        {
            _grassMaterial = grassMaterial;

            _trample = new GrassTrample();
            _trample.Setup(ctx.Config.trample, ctx.Canvas);

            grassMaterial.SetTexture(ShaderIDs.TrampleMap, _trample.OutputTexture);
        }


        public void OnUpdate(float deltaTime)
        {
            _trample.Update(deltaTime);
            _trample.UploadSmoothBrushesTo(_grassMaterial);
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

using UnityEngine;

namespace Snm.WaterSystem.Foam
{
    public class FoamShaderBinder
    {
        private static readonly int FoamTexID = Shader.PropertyToID("_FoamTex");
        private static readonly int FoamStrengthID = Shader.PropertyToID("_FoamStrength");
        private static readonly int FoamDepthThresholdID = Shader.PropertyToID("_FoamDepthThreshold");
        private static readonly int FoamScaleID = Shader.PropertyToID("_FoamScale");
        private static readonly int FoamSpeedID = Shader.PropertyToID("_FoamSpeed");

        private readonly Material _material;

        public FoamShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(Texture2D texture, float strength, float depthThreshold, float scale, float speed)
        {
            if (texture != null)
                _material.SetTexture(FoamTexID, texture);

            _material.SetFloat(FoamStrengthID, strength);
            _material.SetFloat(FoamDepthThresholdID, depthThreshold);
            _material.SetFloat(FoamScaleID, scale);
            _material.SetFloat(FoamSpeedID, speed);
        }
    }
}

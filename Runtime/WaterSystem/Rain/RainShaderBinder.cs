using UnityEngine;

namespace Snm.WaterSystem.Rain
{
    public class RainShaderBinder
    {
        private static readonly int RippleTexID = Shader.PropertyToID("_RainRippleTex");
        private static readonly int IntensityID = Shader.PropertyToID("_RainIntensity");
        private static readonly int DensityID = Shader.PropertyToID("_RainDensity");
        private static readonly int SpeedID = Shader.PropertyToID("_RainSpeed");
        private static readonly int ScaleID = Shader.PropertyToID("_RainScale");

        private readonly Material _material;

        public RainShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(Texture2D texture, float intensity, float density, float speed, float scale)
        {
            if (texture != null)
                _material.SetTexture(RippleTexID, texture);

            _material.SetFloat(IntensityID, intensity);
            _material.SetFloat(DensityID, density);
            _material.SetFloat(SpeedID, speed);
            _material.SetFloat(ScaleID, scale);
        }
    }
}

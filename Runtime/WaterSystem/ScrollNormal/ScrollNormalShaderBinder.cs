using UnityEngine;

namespace Snm.WaterSystem.ScrollNormal
{
    public class ScrollNormalShaderBinder
    {
        private static readonly int NormalMapID = Shader.PropertyToID("_ScrollNormalMap");
        private static readonly int StrengthID = Shader.PropertyToID("_ScrollNormalStrength");
        private static readonly int ScaleID = Shader.PropertyToID("_ScrollNormalScale");
        private static readonly int Speed1ID = Shader.PropertyToID("_ScrollNormalSpeed1");
        private static readonly int Speed2ID = Shader.PropertyToID("_ScrollNormalSpeed2");

        private readonly Material _material;

        public ScrollNormalShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(Texture2D normalMap, float strength, float scale, Vector2 speed1, Vector2 speed2)
        {
            if (normalMap != null)
                _material.SetTexture(NormalMapID, normalMap);

            _material.SetFloat(StrengthID, strength);
            _material.SetFloat(ScaleID, scale);
            _material.SetVector(Speed1ID, speed1);
            _material.SetVector(Speed2ID, speed2);
        }
    }
}

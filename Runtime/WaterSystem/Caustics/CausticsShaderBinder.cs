using UnityEngine;

namespace Snm.WaterSystem.Caustics
{
    public class CausticsShaderBinder
    {
        private static readonly int CausticsTexID = Shader.PropertyToID("_CausticsTex");
        private static readonly int CausticStrengthID = Shader.PropertyToID("_CausticStrength");
        private static readonly int CausticScaleID = Shader.PropertyToID("_CausticScale");
        private static readonly int CausticSpeedID = Shader.PropertyToID("_CausticSpeed");
        private static readonly int CausticSplitID = Shader.PropertyToID("_CausticSplit");

        private readonly Material _material;

        public CausticsShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(Texture2D texture, float strength, float scale, float speed, float split)
        {
            if (texture != null)
                _material.SetTexture(CausticsTexID, texture);

            _material.SetFloat(CausticStrengthID, strength);
            _material.SetFloat(CausticScaleID, scale);
            _material.SetFloat(CausticSpeedID, speed);
            _material.SetFloat(CausticSplitID, split);
        }
    }
}

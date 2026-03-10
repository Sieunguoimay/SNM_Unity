using UnityEngine;

namespace Snm.WaterSystem.Depth
{
    public class DepthShaderBinder
    {
        private static readonly int ShallowColorID = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorID = Shader.PropertyToID("_DeepColor");
        private static readonly int AbsorptionID = Shader.PropertyToID("_Absorption");

        private readonly Material _material;

        public DepthShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(Color shallowColor, Color deepColor, float absorption)
        {
            _material.SetColor(ShallowColorID, shallowColor);
            _material.SetColor(DeepColorID, deepColor);
            _material.SetFloat(AbsorptionID, absorption);
        }
    }
}

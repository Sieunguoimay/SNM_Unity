using UnityEngine;

namespace Snm.WaterSystem.IntersectionBands
{
    public class IntersectionBandsShaderBinder
    {
        private static readonly int BandCountID = Shader.PropertyToID("_BandCount");
        private static readonly int SpeedID = Shader.PropertyToID("_BandSpeed");
        private static readonly int StrengthID = Shader.PropertyToID("_BandStrength");
        private static readonly int SharpnessID = Shader.PropertyToID("_BandSharpness");
        private static readonly int MaxDepthID = Shader.PropertyToID("_BandMaxDepth");

        private readonly Material _material;

        public IntersectionBandsShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(int lineCount, float speed, float strength, float sharpness, float maxDepth)
        {
            _material.SetInt(BandCountID, lineCount);
            _material.SetFloat(SpeedID, speed);
            _material.SetFloat(StrengthID, strength);
            _material.SetFloat(SharpnessID, sharpness);
            _material.SetFloat(MaxDepthID, maxDepth);
        }
    }
}

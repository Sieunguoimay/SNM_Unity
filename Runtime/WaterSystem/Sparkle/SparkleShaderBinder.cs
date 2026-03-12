using UnityEngine;

namespace Snm.WaterSystem.Sparkle
{
    public class SparkleShaderBinder
    {
        private static readonly int IntensityID = Shader.PropertyToID("_SparkleIntensity");
        private static readonly int DensityID = Shader.PropertyToID("_SparkleDensity");
        private static readonly int SpeedID = Shader.PropertyToID("_SparkleSpeed");

        private readonly Material _material;

        public SparkleShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(float intensity, float density, float speed)
        {
            _material.SetFloat(IntensityID, intensity);
            _material.SetFloat(DensityID, density);
            _material.SetFloat(SpeedID, speed);
        }
    }
}

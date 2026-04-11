using UnityEngine;

namespace Snm.WaterSystem.Shoreline
{
    public class ShorelineShaderBinder
    {
        private static readonly int WaveCountID = Shader.PropertyToID("_ShorelineWaveCount");
        private static readonly int SpeedID = Shader.PropertyToID("_ShorelineSpeed");
        private static readonly int FoamStrengthID = Shader.PropertyToID("_ShorelineFoamStrength");
        private static readonly int FoamScaleID = Shader.PropertyToID("_ShorelineFoamScale");

        private readonly Material _material;

        public ShorelineShaderBinder(Material material)
        {
            _material = material;
        }

        public void Bind(int waveCount, float speed, float foamStrength, float foamScale)
        {
            _material.SetInt(WaveCountID, waveCount);
            _material.SetFloat(SpeedID, speed);
            _material.SetFloat(FoamStrengthID, foamStrength);
            _material.SetFloat(FoamScaleID, foamScale);
        }
    }
}

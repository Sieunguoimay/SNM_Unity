using System;
using Snm.WaterSystem.Caustics;
using Snm.WaterSystem.Depth;
using Snm.WaterSystem.Reflection;
using Snm.WaterSystem.Surface;
using Snm.WaterSystem.Wave;

namespace Snm.WaterSystem
{
    [Serializable]
    public class WaterConfig
    {
        public SurfaceConfig surface = new();
        public ReflectionConfig reflection = new();
        public CausticsConfig caustics = new();
        public WaterDepthConfig depth = new();
        public WaveConfig wave = new();
    }
}
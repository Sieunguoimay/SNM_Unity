using System;
using Snm.WaterSystem.Caustics;
using Snm.WaterSystem.Depth;
using Snm.WaterSystem.Foam;
using Snm.WaterSystem.Reflection;
using Snm.WaterSystem.ScrollNormal;
using Snm.WaterSystem.Shoreline;
using Snm.WaterSystem.Sparkle;
using Snm.WaterSystem.Surface;
using Snm.WaterSystem.Buoyancy;
using Snm.WaterSystem.IntersectionBands;
using Snm.WaterSystem.Wave;

namespace Snm.WaterSystem
{
    [Serializable]
    public class WaterConfig
    {
        public SurfaceConfig surface = new();
        public WaveConfig wave = new();
        public ReflectionConfig reflection = new();
        public DepthConfig depth = new();
        public CausticsConfig caustics = new();
        public FoamConfig foam = new();
        public ShorelineConfig shoreline = new();
        public SparkleConfig sparkle = new();
        public ScrollNormalConfig scrollNormal = new();
        public BuoyancyConfig buoyancy = new();
        public IntersectionBandsConfig intersectionBands = new();
    }
}

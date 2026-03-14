using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem
{
    public sealed class WaterFeatureContext
    {
        public readonly WaterConfig Config;
        public readonly SurfaceData Surface;
        public readonly Material SurfaceMaterial;
        public readonly Camera SourceCamera;

        public WaterFeatureContext(
            WaterConfig config,
            SurfaceData surface,
            Material surfaceMaterial,
            Camera sourceCamera)
        {
            Config = config;
            Surface = surface;
            SurfaceMaterial = surfaceMaterial;
            SourceCamera = sourceCamera;
        }
    }
}

using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem
{
    public sealed class WaterFeatureContext
    {
        public readonly WaterConfig Config;
        public readonly SurfaceCanvas Surface;
        public readonly Material SurfaceMaterial;
        public readonly Camera SourceCamera;

        public WaterFeatureContext(
            WaterConfig config,
            SurfaceCanvas surface,
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

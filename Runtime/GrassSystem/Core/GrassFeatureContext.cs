using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.GrassSystem
{
    public sealed class GrassFeatureContext
    {
        public readonly GrassSystemConfig Config;
        public readonly SurfaceCanvas Canvas;
        public readonly Material GrassMaterial;

        public GrassFeatureContext(
            GrassSystemConfig config,
            SurfaceCanvas canvas,
            Material grassMaterial)
        {
            Config = config;
            Canvas = canvas;
            GrassMaterial = grassMaterial;
        }
    }
}

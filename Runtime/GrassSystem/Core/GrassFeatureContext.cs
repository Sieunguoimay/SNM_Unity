using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.GrassSystem
{
    public sealed class GrassFeatureContext
    {
        public readonly GrassSystemConfig Config;
        public readonly SurfaceCanvas Canvas;
        public readonly Material GrassMaterial;
        public readonly IReadOnlyList<Material> AllMaterials;

        public GrassFeatureContext(
            GrassSystemConfig config,
            SurfaceCanvas canvas,
            Material grassMaterial,
            IReadOnlyList<Material> allMaterials = null)
        {
            Config = config;
            Canvas = canvas;
            GrassMaterial = grassMaterial;
            AllMaterials = allMaterials ?? new[] { grassMaterial };
        }
    }
}

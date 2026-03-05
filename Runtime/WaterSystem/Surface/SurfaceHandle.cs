// ═══════════════════════════════════════════════════════════════
// WaterSurfaceRuntime.cs
// Creates and owns the MeshRenderer GameObject for the water quad.
// Applies material property updates each frame via the binder.
// ═══════════════════════════════════════════════════════════════
using System;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    public class SurfaceHandle : IDisposable
    {
        private readonly SurfaceData surface;
        private readonly SurfaceRenderer renderer;
        private readonly Material material;
        private readonly IDisposable disposable;

        public SurfaceData Surface => surface;
        public SurfaceRenderer Renderer => renderer;
        public Material Material => material;

        public SurfaceHandle(
            SurfaceData surface,
            SurfaceRenderer renderer,
            Material material,
            IDisposable disposable)
        {
            this.surface = surface;
            this.renderer = renderer;
            this.material = material;
            this.disposable = disposable;
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}

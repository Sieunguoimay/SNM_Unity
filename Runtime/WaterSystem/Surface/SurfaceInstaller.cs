// ═══════════════════════════════════════════════════════════════
// WaterSurfaceInstaller.cs
// Creates and owns the MeshRenderer GameObject for the water quad.
// Applies material property updates each frame via the binder.
// ═══════════════════════════════════════════════════════════════
using Snm.Runtime.Dispose;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    public static class SurfaceInstaller
    {
        public static SurfaceHandle Install(
            GameObject context,
            WaterConfig config,
            IUpdateService updateService)
        {
            // ── water surface data ───────────────────────────────────────────
            var surface = new SurfaceData
            {
                size = config.waterSurfaceSize,
                mesh = config.autoGenerateMesh
                    ? SurfaceMeshBuilder.CreateQuad(config.waterSurfaceSize)
                    : config.mesh,
            };

            // ── scene bridge: keeps surface.Position/Rotation in sync ────────
            var surfaceMB = new GameObject("[WaterSurface]").AddComponent<WaterSurfaceMB>();
            surfaceMB.transform.SetParent(context.transform);
            surfaceMB.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            surfaceMB.Bind(surface);

            // ── material ─────────────────────────────────────────────────────
            bool ownsMaterial = config.waterSurfaceMaterial == null;
            var material = ownsMaterial ? new Material(config.waterSurfaceShader) : config.waterSurfaceMaterial;

            // ── surface renderer ─────────────────────────────────────────────
            var surfaceRenderer = new SurfaceRenderer(surface, material);
            updateService.AddUpdateTarget(surfaceRenderer);

            return new SurfaceHandle(
                surface,
                surfaceRenderer,
                material,
                disposable: new DisposeCallback(() =>
            {
                surfaceRenderer.Dispose();
                if (ownsMaterial) Object.Destroy(material);
                Object.Destroy(surfaceMB.gameObject);
            }));
        }
    }
}

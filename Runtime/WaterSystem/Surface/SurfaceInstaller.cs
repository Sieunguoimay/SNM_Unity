// ═══════════════════════════════════════════════════════════════
// SurfaceInstaller.cs
// Creates and owns the MeshRenderer GameObject for the water quad.
// Applies material property updates each frame via the binder.
// ═══════════════════════════════════════════════════════════════
using System;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    public static class SurfaceInstaller
    {
        internal static (SurfaceData surface, Material material, IDisposable cleanup) Install(
            SurfaceConfig config,
            IUpdateService updateService)
        {
            // ── water surface data ───────────────────────────────────────────
            var surface = new SurfaceData
            {
                size = config.size,
                mesh = config.autoGenerateMesh
                    ? SurfaceMeshBuilder.CreateQuad(config.size)
                    : config.mesh,
            };

            // ── scene bridge: keeps surface.Position/Rotation in sync ────────
            var surfaceMB = new GameObject("[WaterSurface]").AddComponent<SurfaceMB>();
            surfaceMB.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            surfaceMB.Bind(surface);

            // ── material ─────────────────────────────────────────────────────
            bool ownsMaterial = config.waterSurfaceMaterial == null;
            var material = ownsMaterial ? new Material(config.waterSurfaceShader) : config.waterSurfaceMaterial;

            // ── surface renderer ─────────────────────────────────────────────
            var surfaceRenderer = new SurfaceRenderer(surface, material);
            updateService.AddUpdateTarget(surfaceRenderer);

            return (
                surface,
                material,
                cleanup: new DisposeCallback(() =>
                {
                    surfaceRenderer.Dispose();
                    if (ownsMaterial) UnityEngineUtility.DestroyObject(material);
                    if(surfaceMB) UnityEngineUtility.DestroyObject(surfaceMB.gameObject);
                }));
        }
    }
}

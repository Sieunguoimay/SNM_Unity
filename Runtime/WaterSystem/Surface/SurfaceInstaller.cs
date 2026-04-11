using System;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    public static class SurfaceInstaller
    {
        internal static (SurfaceCanvas canvas, Material material, IDisposable cleanup) Install(
            SurfaceConfig config,
            Transform parent = null,
            Mesh meshOverride = null)
        {
            Mesh mesh;
            if (meshOverride != null)
                mesh = meshOverride;
            else if (config.autoGenerateMesh)
                mesh = SurfaceMeshBuilder.CreateQuad(config.size);
            else
                mesh = config.mesh;

            bool ownsMaterial = config.waterSurfaceMaterial == null;
            var material = ownsMaterial ? new Material(config.waterSurfaceShader) : config.waterSurfaceMaterial;

            var go = new GameObject("[WaterSurface]");
            if (parent != null) go.transform.SetParent(parent, false);
            var surfaceMB = go.AddComponent<WaterSurfaceMB>();
            surfaceMB.Setup(mesh, material, config.size);

            return (
                surfaceMB.Canvas,
                material,
                new Snm.Runtime.Dispose.DisposeCallback(() =>
                {
                    if (ownsMaterial) UnityEngineUtility.DestroyObject(material);
                    if (go) UnityEngineUtility.DestroyObject(go);
                }));
        }
    }
}

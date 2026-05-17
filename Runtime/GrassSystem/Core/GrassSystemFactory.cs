using System.Collections.Generic;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;

#pragma warning disable 618

namespace Snm.GrassSystem
{
    public static class GrassSystemFactory
    {
        public static GrassSystemHandle Create(
            GrassSystemConfig config,
            Matrix4x4[] matrices,
            Matrix4x4[][] layerMatrices,
            SurfaceCanvas canvas,
            Bounds worldBounds)
        {
            var canvasVec = BuildCanvasVector(canvas);
            var renderers = new List<GrassRenderer>();
            var allMaterials = new List<Material>();

            if (config.HasLayers)
            {
                for (int i = 0; i < config.layers.Length; i++)
                {
                    var layer = config.layers[i];
                    if (layerMatrices[i].Length == 0) continue;

                    var r = CreateRenderer(layer.mesh, layer.material, layerMatrices[i], worldBounds, canvasVec, layer.mesh.bounds.size.y);
                    renderers.Add(r);
                    allMaterials.Add(r.Material);
                }
            }
            else
            {
                var r = CreateRenderer(config.grassMesh, config.grassMaterial, matrices, worldBounds, canvasVec, config.bladeHeight);
                renderers.Add(r);
                allMaterials.Add(r.Material);
            }

            return BuildHandle(config, canvas, renderers, allMaterials);
        }

        public static GrassSystemHandle CreateFromPatches(
            GrassSystemConfig config,
            List<GrassPatchCollector.RenderGroup> renderGroups,
            SurfaceCanvas canvas,
            Bounds worldBounds)
        {
            var canvasVec = BuildCanvasVector(canvas);
            var renderers = new List<GrassRenderer>();
            var allMaterials = new List<Material>();

            foreach (var group in renderGroups)
            {
                if (group.Matrices.Length == 0) continue;

                var r = CreateRenderer(group.Mesh, group.Material, group.Matrices, worldBounds, canvasVec, group.Mesh.bounds.size.y);
                renderers.Add(r);
                allMaterials.Add(r.Material);
            }

            if (renderers.Count == 0) return null;

            return BuildHandle(config, canvas, renderers, allMaterials);
        }

        static Vector4 BuildCanvasVector(SurfaceCanvas canvas)
        {
            var worldMin = canvas.WorldMin;
            return new Vector4(worldMin.x, worldMin.y, canvas.Size.x, canvas.Size.y);
        }

        static GrassRenderer CreateRenderer(Mesh mesh, Material material, Matrix4x4[] matrices, Bounds worldBounds, Vector4 canvasVec, float bladeHeight)
        {
            var r = new GrassRenderer();
            r.Setup(mesh, material, matrices, worldBounds);
            r.SetWorldCanvas(canvasVec);
            r.SetBladeHeight(bladeHeight);
            return r;
        }

        static GrassSystemHandle BuildHandle(
            GrassSystemConfig config,
            SurfaceCanvas canvas,
            List<GrassRenderer> renderers,
            List<Material> allMaterials)
        {
            var primaryRenderer = renderers.Count > 0 ? renderers[0] : null;
            var primaryMaterial = allMaterials.Count > 0 ? allMaterials[0] : null;
            var ctx = new GrassFeatureContext(config, canvas, primaryMaterial, allMaterials);

            var composite = new GrassFeatureComposite();

            if (config.ambientOcclusion.enabled)
                composite.Add(new AmbientOcclusionFeature(ctx));

            if (config.colorVariation.enabled)
                composite.Add(new ColorVariationFeature(ctx));

            if (config.wind.enabled)
                composite.Add(new WindFeature(ctx));
            else
                WindFeature.ClearWindProperties(ctx.AllMaterials);

            TrampleFeature trampleFeature = null;
            if (config.trample.enabled)
            {
                trampleFeature = new TrampleFeature(ctx);
                composite.Add(trampleFeature);
            }

            if (config.trample.enabled && config.trample.springEnabled)
                composite.Add(new RecoverySpringFeature(ctx));

            if (config.frustumCulling.enabled)
                composite.Add(new FrustumCullingFeature(renderers, config.frustumCulling.margin, ctx.MainCameraProvider));

            // Render must be last — all features update before draw
            foreach (var r in renderers)
                composite.Add(new RenderFeature(r));

            var updater = new GameObject("[GrassUpdater]").AddComponent<UpdateDispatcher>();
            updater.AddUpdateTarget(composite);

            var disposeList = new List<System.IDisposable> { composite };
            disposeList.AddRange(renderers);
            disposeList.Add(new DisposeCallback(() =>
            {
                if (updater) UnityEngineUtility.DestroyObject(updater.gameObject);
            }));

            var cleanup = new DisposeCollection(disposeList.ToArray());

            return new GrassSystemHandle(cleanup, primaryRenderer, trampleFeature?.Trample, canvas);
        }
    }
}

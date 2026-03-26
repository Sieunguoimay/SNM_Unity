using System.Collections.Generic;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;

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
            var worldMin = canvas.WorldMin;
            var canvasVec = new Vector4(worldMin.x, worldMin.y, canvas.Size.x, canvas.Size.y);

            var renderers = new List<GrassRenderer>();
            var allMaterials = new List<Material>();

            if (config.HasLayers)
            {
                for (int i = 0; i < config.layers.Length; i++)
                {
                    var layer = config.layers[i];
                    if (layerMatrices[i].Length == 0) continue;

                    var r = new GrassRenderer();
                    r.Setup(layer.mesh, layer.material, layerMatrices[i], worldBounds);
                    r.SetWorldCanvas(canvasVec);

                    float bladeHeight = layer.mesh.bounds.size.y;
                    r.SetBladeHeight(bladeHeight);

                    renderers.Add(r);
                    allMaterials.Add(r.Material);
                }
            }
            else
            {
                var r = new GrassRenderer();
                r.Setup(config.grassMesh, config.grassMaterial, matrices, worldBounds);
                r.SetWorldCanvas(canvasVec);
                r.SetBladeHeight(config.bladeHeight);

                renderers.Add(r);
                allMaterials.Add(r.Material);
            }

            // Primary renderer (first layer or single)
            var primaryRenderer = renderers.Count > 0 ? renderers[0] : null;

            // --- context (all materials for shared features) ---
            var primaryMaterial = allMaterials.Count > 0 ? allMaterials[0] : null;
            var ctx = new GrassFeatureContext(config, canvas, primaryMaterial, allMaterials);

            // --- features ---
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
                composite.Add(new FrustumCullingFeature(renderers, config.frustumCulling.margin));

            // Render must be last — all features update before draw
            foreach (var r in renderers)
                composite.Add(new RenderFeature(r));

            // --- update dispatcher ---
            var updater = new GameObject("[GrassUpdater]").AddComponent<UpdateDispatcher>();
            updater.AddUpdateTarget(composite);

            // --- cleanup ---
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

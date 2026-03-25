using System.Collections.Generic;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using Snm.WaterSystem;
using UnityEngine;

namespace Snm.GrassSystem
{
    public static class GrassSystemFactory
    {
        public static GrassSystemHandle Create(
            GrassSystemConfig config,
            Matrix4x4[] matrices,
            SurfaceCanvas canvas,
            Bounds worldBounds)
        {
            var worldMin = canvas.WorldMin;
            var canvasVec = new Vector4(worldMin.x, worldMin.y, canvas.Size.x, canvas.Size.y);

            // --- renderer ---
            var renderer = new GrassRenderer();
            renderer.Setup(config.grassMesh, config.grassMaterial, matrices, worldBounds);
            renderer.SetWorldCanvas(canvasVec);

            // --- context ---
            var ctx = new GrassFeatureContext(config, canvas, renderer.Material);

            // --- features ---
            var composite = new GrassFeatureComposite();

            if (config.wind.enabled)
                composite.Add(new WindFeature(ctx));

            TrampleFeature trampleFeature = null;
            if (config.trample.enabled)
            {
                trampleFeature = new TrampleFeature(ctx, renderer.Material);
                composite.Add(trampleFeature);
            }

            // Render must be last — all features update before draw
            composite.Add(new RenderFeature(renderer));

            // --- update dispatcher ---
            var updater = new GameObject("[GrassUpdater]").AddComponent<UpdateDispatcher>();
            updater.AddUpdateTarget(composite);

            // --- cleanup ---
            var cleanup = new DisposeCollection(
                composite,
                renderer,
                new DisposeCallback(() =>
                {
                    if (updater) UnityEngineUtility.DestroyObject(updater.gameObject);
                }));

            return new GrassSystemHandle(cleanup, renderer, trampleFeature?.Trample, canvas);
        }
    }
}

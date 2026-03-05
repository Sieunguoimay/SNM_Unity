
// ═══════════════════════════════════════════════════════════════
// WaterSystemInstaller.cs
// Composition root for the entire water system.
// The only place that knows how all pieces connect.
// ═══════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using Snm.Runtime.Dispose;
using Snm.WaterSystem.Reflection;
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem
{
    public static class WaterSystemInstaller
    {
        public static WaterSystemHandle Install(
            GameObject context,
            WaterConfig config,
            Camera sourceCamera)
        {
            var updater = new GameObject("[WaterUpdater]").AddComponent<UpdateDispatcher>();

            var waterSurface = SurfaceInstaller.Install(context, config, updater);

            var ctx = new WaterFeatureContext(
                config,
                waterSurface.Surface,
                waterSurface.Material,
                sourceCamera,
                updater);

            // ── features ──────────────────────────────────────────────────────
            // Add a feature  = add one line.
            // Remove a feature = remove/comment one line.
            // Each feature wires itself to the surface material internally.
            var features = new List<IDisposable>
            {
                ReflectionInstaller.Install(ctx)
            };
            // features.Add(RefractionInstaller.Install(ctx));
            // features.Add(KelvinWakeInstaller.Install(ctx));

            return new WaterSystemHandle(
                new DisposeCallback(() =>
                {
                    foreach (var f in features) f.Dispose();
                    waterSurface.Dispose();
                    UnityEngine.Object.Destroy(updater.gameObject);
                }));
        }
    }
}


// ═══════════════════════════════════════════════════════════════
// WaterSystemFactory.cs
// Composition root for the entire water system.
// The only place that knows how all pieces connect.
// ═══════════════════════════════════════════════════════════════
using System.Collections.Generic;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using Snm.WaterSystem.Caustics;
using Snm.WaterSystem.Depth;
using Snm.WaterSystem.Foam;
using Snm.WaterSystem.Rain;
using Snm.WaterSystem.Reflection;
using Snm.WaterSystem.ScrollNormal;
using Snm.WaterSystem.Shoreline;
using Snm.WaterSystem.Sparkle;
using Snm.WaterSystem.Surface;
using Snm.WaterSystem.Wave;
using UnityEngine;

namespace Snm.WaterSystem
{
    public static class WaterSystemFactory
    {
        /// <param name="disturbers">
        ///   Optional live enumerable of <see cref="IWaveDisturber"/> objects.
        ///   When provided and <c>config.disturber.enabled</c> is true,
        ///   the system will generate ripples whenever a disturber enters or moves through the water.
        /// </param>
        public static WaterSystemHandle Create(
            WaterConfig config,
            Camera sourceCamera,
            IEnumerable<IWaveDisturber> disturbers = null,
            Transform parent = null)
        {
            var updater = new GameObject("[WaterUpdater]").AddComponent<UpdateDispatcher>();
            var (canvas, surfaceMaterial, surfaceCleanup) = SurfaceInstaller.Install(config.surface, parent);

            var ctx = new WaterFeatureContext(
                config,
                canvas,
                surfaceMaterial,
                sourceCamera);

            // ── shader keywords ───────────────────────────────────────────
            if (config.caustics.enabled) surfaceMaterial.EnableKeyword("_CAUSTICS_ON");
            if (config.caustics.chromaticSplit) surfaceMaterial.EnableKeyword("_CAUSTICS_CHROMATIC");
            if (config.reflection.enabled) surfaceMaterial.EnableKeyword("_REFLECTION_ON");
            if (config.foam.enabled) surfaceMaterial.EnableKeyword("_FOAM_ON");
            if (config.shoreline.enabled) surfaceMaterial.EnableKeyword("_SHORELINE_ON");
            if (config.sparkle.enabled) surfaceMaterial.EnableKeyword("_SPARKLE_ON");
            if (config.scrollNormal.enabled) surfaceMaterial.EnableKeyword("_SCROLL_NORMAL_ON");
            surfaceMaterial.EnableKeyword("_SPECULAR_ON");

            // ── features ──────────────────────────────────────────────────
            var composite = new WaterFeatureComposite();

            ReflectionFeature reflection = null;
            if (config.reflection.enabled)
            {
                reflection = ReflectionInstaller.Install(ctx);
                composite.Add(reflection);
            }

            if (config.caustics.enabled)
                composite.Add(new CausticsFeature(ctx.SurfaceMaterial, ctx.Config.caustics));

            if (config.depth.enabled)
                composite.Add(new DepthFeature(ctx.SurfaceMaterial, ctx.Config.depth));

            IWaveSimulation waveSim = null;
            if (config.wave.enabled)
            {
                waveSim = WaveSimulationFactory.Create(
                    config.waveSimulation,
                    config.wave.textureSize,
                    config.wave.simulationShader,
                    config.wave.displayShader,
                    ctx.SurfaceMaterial);
                composite.Add((IWaterFeature)waveSim);
            }

            if (config.foam.enabled) composite.Add(new FoamFeature(ctx.SurfaceMaterial, ctx.Config.foam));
            if (config.shoreline.enabled) composite.Add(new ShorelineFeature(ctx.SurfaceMaterial, ctx.Config.shoreline));
            if (config.sparkle.enabled) composite.Add(new SparkleFeature(ctx.SurfaceMaterial, ctx.Config.sparkle));
            if (config.scrollNormal.enabled) composite.Add(new ScrollNormalFeature(ctx.SurfaceMaterial, ctx.Config.scrollNormal));
            if (config.rain.enabled && config.wave.enabled) composite.Add(new RainFeature(waveSim, ctx.Config.rain));

            WaveDisturberTracker disturberTracker = null;
            bool useDisturbers = config.disturber.enabled && config.wave.enabled && disturbers != null;
            if (useDisturbers)
            {
                disturberTracker = new WaveDisturberTracker(disturbers, canvas, waveSim, ctx.Config.disturber);
                composite.Add(new WaveDisturberFeature(disturberTracker));
            }

            updater.AddUpdateTarget(composite);
            updater.AddLateUpdateTarget(composite);

            // ── cleanup ───────────────────────────────────────────────────
            var cleanup = new DisposeCollection(
                composite,
                surfaceCleanup,
                new DisposeCallback(() =>
                {
                    if (updater) UnityEngineUtility.DestroyObject(updater.gameObject);
                }));

            return new WaterSystemHandle(
                cleanup,
                reflection?.Texture,
                waveSim,
                disturberTracker);
        }
    }
}

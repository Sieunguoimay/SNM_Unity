
// ═══════════════════════════════════════════════════════════════
// WaterSystemInstaller.cs
// Composition root for the entire water system.
// The only place that knows how all pieces connect.
// ═══════════════════════════════════════════════════════════════
using Snm.DependencyInjection;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
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
    public static class WaterSystemInstaller
    {
        public static WaterSystemHandle Install(
            WaterConfig config,
            Camera sourceCamera)
        {
            var updater = new GameObject("[WaterUpdater]").AddComponent<UpdateDispatcher>();
            var (surface, surfaceMaterial, surfaceCleanup) = SurfaceInstaller.Install(config.surface, updater);

            var ctx = new WaterFeatureContext(
                config,
                surface,
                surfaceMaterial,
                sourceCamera,
                updater);

            // ── DI container ─────────────────────────────────────────────────
            var builder = new ContainerBuilder();

            // shared infrastructure
            builder.Bind<IUpdateService>().ToInstance(updater);
            builder.Bind<WaterFeatureContext>().ToInstance(ctx);

            // ── features ─────────────────────────────────────────────────────
            // Add a feature = add one line + config toggle.
            // ── shader keywords ───────────────────────────────────────────
            if (config.caustics.enabled)       surfaceMaterial.EnableKeyword("_CAUSTICS_ON");
            if (config.caustics.chromaticSplit) surfaceMaterial.EnableKeyword("_CAUSTICS_CHROMATIC");
            if (config.reflection.enabled)     surfaceMaterial.EnableKeyword("_REFLECTION_ON");
            if (config.foam.enabled)           surfaceMaterial.EnableKeyword("_FOAM_ON");
            if (config.shoreline.enabled)      surfaceMaterial.EnableKeyword("_SHORELINE_ON");
            if (config.sparkle.enabled)        surfaceMaterial.EnableKeyword("_SPARKLE_ON");
            if (config.scrollNormal.enabled)   surfaceMaterial.EnableKeyword("_SCROLL_NORMAL_ON");
            surfaceMaterial.EnableKeyword("_SPECULAR_ON");

            if (config.reflection.enabled)     ReflectionInstaller.Install(builder);
            if (config.caustics.enabled)       CausticsInstaller.Install(builder);
            if (config.depth.enabled)          DepthInstaller.Install(builder);
            if (config.wave.enabled)           WaveSimulationInstaller.Install(
                                                   builder,
                                                   config.wave.textureSize,
                                                   config.wave.simulationShader,
                                                   config.wave.displayShader);
            if (config.foam.enabled)           FoamInstaller.Install(builder);
            if (config.shoreline.enabled)      ShorelineInstaller.Install(builder);
            if (config.sparkle.enabled)        SparkleInstaller.Install(builder);
            if (config.scrollNormal.enabled)   ScrollNormalInstaller.Install(builder);
            if (config.rain.enabled && config.wave.enabled) RainInstaller.Install(builder);

            // cleanup for non-DI owned resources
            builder.Bind<DisposeCallback>().ToScoped(_ =>
                new DisposeCallback(() =>
                {
                    surfaceCleanup.Dispose();
                    if(updater) UnityEngineUtility.DestroyObject(updater.gameObject);
                }));

            var scope = builder.Build();

            // ── collect features into composite ──────────────────────────────
            var composite = new WaterFeatureComposite();

            if (config.reflection.enabled)     composite.Add(scope.Resolve<ReflectionFeature>());
            if (config.caustics.enabled)       composite.Add(scope.Resolve<CausticsFeature>());
            if (config.depth.enabled)          composite.Add(scope.Resolve<DepthFeature>());
            if (config.wave.enabled)           composite.Add((IWaterFeature)scope.Resolve<IWaveSimulation>());
            if (config.foam.enabled)           composite.Add(scope.Resolve<FoamFeature>());
            if (config.shoreline.enabled)      composite.Add(scope.Resolve<ShorelineFeature>());
            if (config.sparkle.enabled)        composite.Add(scope.Resolve<SparkleFeature>());
            if (config.scrollNormal.enabled)   composite.Add(scope.Resolve<ScrollNormalFeature>());
            if (config.rain.enabled && config.wave.enabled) composite.Add(scope.Resolve<RainFeature>());

            updater.AddUpdateTarget(composite);
            updater.AddLateUpdateTarget(composite);

            scope.Resolve<DisposeCallback>();

            var reflectionTexture = config.reflection.enabled
                ? scope.Resolve<ReflectionFeature>().Texture
                : null;

            IWaveSimulation waveSimulation = config.wave.enabled
                ? scope.Resolve<IWaveSimulation>()
                : null;

            return new WaterSystemHandle(scope, reflectionTexture, waveSimulation);
        }
    }
}

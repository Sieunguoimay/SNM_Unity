
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
using Snm.WaterSystem.Reflection;
using Snm.WaterSystem.Surface;
using Snm.WaterSystem.Wave;
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
            var waterSurface = SurfaceInstaller.Install(context, config.surface, updater);

            var ctx = new WaterFeatureContext(
                config,
                waterSurface.Surface,
                waterSurface.Material,
                sourceCamera,
                updater);

            // ── DI container ─────────────────────────────────────────────────
            var builder = new ContainerBuilder();

            // shared infrastructure
            builder.Bind<IUpdateService>().ToInstance(updater);
            builder.Bind<WaterFeatureContext>().ToInstance(ctx);

            // ── features ─────────────────────────────────────────────────────
            // Add a feature = add one line + config toggle.
            if (config.reflection.enabled) ReflectionInstaller.Install(builder);
            if (config.caustics.enabled)   CausticsInstaller.Install(builder);
            if (config.depth.enabled)      DepthInstaller.Install(builder);
            if (config.wave.enabled)       WaveSimulationInstaller.Install(
                                               builder,
                                               config.wave.textureSize,
                                               config.wave.simulationShader,
                                               config.wave.displayShader);

            // cleanup for non-DI owned resources
            builder.Bind<DisposeCallback>().ToScoped(_ =>
                new DisposeCallback(() =>
                {
                    waterSurface.Dispose();
                    UnityEngineUtility.DestroyObject(updater.gameObject);
                }));

            var scope = builder.Build();

            // Force-resolve scoped bindings so they are created and tracked for disposal.
            if (config.reflection.enabled) scope.Resolve<ReflectionHandle>();
            if (config.caustics.enabled)   scope.Resolve<CausticsHandle>();
            if (config.depth.enabled)      scope.Resolve<DepthHandle>();
            if (config.wave.enabled)       scope.Resolve<IWaveSimulation>();
            scope.Resolve<DisposeCallback>();

            var reflectionTexture = config.reflection.enabled
                ? scope.Resolve<ReflectionHandle>().Texture
                : null;

            var waveDisplayTexture = config.wave.enabled
                ? scope.Resolve<IWaveSimulation>().GetDisplayTexture()
                : null;

            return new WaterSystemHandle(scope, reflectionTexture, waveDisplayTexture);
        }
    }
}

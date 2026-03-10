using Snm.DependencyInjection;
using Snm.Runtime.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystem.Wave
{
    public static class WaveSimulationInstaller
    {
        public static void Install(
            IBindingContext container,
            int textureSize,
            Shader simulationShader,
            Shader displayShader)
        {
            var desc = new RenderTextureDescriptor(textureSize, textureSize)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            container.Bind<WaveSimulationConfig>()
                .ToScoped(_ => new WaveSimulationConfig());

            container.Bind<DisturbanceBuffer>()
                .ToScoped(_ => new DisturbanceBuffer());

            container.Bind<IWaveSimulationPass>()
                .ToScoped(r =>
                {
                    var pingPong = new PingPongTexture(desc);
                    var material = new Material(simulationShader);
                    return new WaveSimulationPass(material, pingPong, r.Resolve<DisturbanceBuffer>());
                });

            container.Bind<IWaveDisplayPass>()
                .ToScoped(_ =>
                {
                    var material = new Material(displayShader);
                    return new WaveDisplayPass(material);
                });

            container.Bind<IWaveSimulation>()
                .ToScoped(r =>
                {
                    var displayRT = new RenderTexture(desc)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        useMipMap = false,
                        autoGenerateMips = false
                    };
                    displayRT.Create();

                    return new WaveSimulationController(
                        r.Resolve<IWaveSimulationPass>(),
                        r.Resolve<IWaveDisplayPass>(),
                        r.Resolve<DisturbanceBuffer>(),
                        displayRT,
                        r.Resolve<WaveSimulationConfig>(),
                        r.Resolve<IUpdateService>());
                });
        }
    }
}

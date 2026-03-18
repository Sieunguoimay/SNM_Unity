using Snm.SurfaceInteraction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystem.Wave
{
    public static class WaveSimulationFactory
    {
        public static WaveSimulationController Create(
            WaveSimulationConfig config,
            int textureSize,
            Shader simulationShader,
            Shader displayShader,
            Material surfaceMaterial = null)
        {
            var desc = new RenderTextureDescriptor(textureSize, textureSize)
            {
                graphicsFormat = GraphicsFormat.R16G16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            var stampBuffer = new StampBuffer(32);
            var pingPong = new PingPongTexture(desc);
            var simMaterial = new Material(simulationShader);
            var stampRenderer = new SurfaceStampRenderer(simMaterial, pingPong);

            var simPass = new WaveSimulationPass(stampRenderer, stampBuffer);

            var displayMaterial = new Material(displayShader);
            var displayPass = new WaveDisplayPass(displayMaterial);

            var displayRT = new RenderTexture(desc)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            displayRT.Create();

            return new WaveSimulationController(
                simPass,
                displayPass,
                stampBuffer,
                displayRT,
                config,
                surfaceMaterial);
        }
    }
}

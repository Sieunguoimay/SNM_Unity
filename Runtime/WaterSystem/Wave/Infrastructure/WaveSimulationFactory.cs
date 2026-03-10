using Snm.Runtime.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystem.Wave
{
    public static class WaveSimulationFactory
    {
        public static WaveSimulationController Create(
            int textureSize,
            Shader simShader,
            Shader displayShader,
            IUpdateService updater)
        {
            var desc = new RenderTextureDescriptor(textureSize, textureSize)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            var config = new WaveSimulationConfig();
            var disturbanceBuffer = new DisturbanceBuffer();
            var pingPong = new PingPongTexture(desc);

            var simMaterial = new Material(simShader);
            var displayMaterial = new Material(displayShader);

            var simPass = new WaveSimulationPass(simMaterial, pingPong, disturbanceBuffer);
            var displayPass = new WaveDisplayPass(displayMaterial);

            var displayRT = new RenderTexture(desc);
            displayRT.filterMode = FilterMode.Bilinear;
            displayRT.wrapMode = TextureWrapMode.Clamp;
            displayRT.useMipMap = false;
            displayRT.autoGenerateMips = false;
            displayRT.Create();

            return new WaveSimulationController(
                simPass,
                displayPass,
                disturbanceBuffer,
                displayRT,
                config,
                updater);
        }
    }
}

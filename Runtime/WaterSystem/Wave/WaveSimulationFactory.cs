using Snm.Runtime.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystem.Wave
{
    public static class WaveSimulationFactory
    {
        public static WaveSimulationController Create(
            WaveSimulationConfig config,
            int textureSize,
            Material simMaterial,
            Material displayMaterial,
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

            // var config = new WaveSimulationConfig();
            var disturbanceBuffer = new DisturbanceBuffer();
            var pingPong = new PingPongTexture(desc);

            var simPass = new WaveSimulationPass(simMaterial, pingPong, disturbanceBuffer);
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
                disturbanceBuffer,
                displayRT,
                config,
                surfaceMaterial);
        }
    }
}

using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemInstaller
    {
        public GrassTrampleSystemHandle Install(
            GrassTrampleSystemConfig config,
            int textureSize,
            SurfaceCanvas canvas)
        {
            var desc = new RenderTextureDescriptor(textureSize, textureSize)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };
            var pingPong = new PingPongTexture(desc);
            var previewTexture = pingPong.A;
            var material = new UnityEngine.Material(config.shader);
            var stampRenderer = new SurfaceStampRenderer(material, pingPong);
            var stampBuffer = new StampBuffer(64);

            var renderer = new GrassTrampleRenderer(stampRenderer, stampBuffer, canvas, config.fadeSpeed);

            var tracker = new GrassDisturberTracker(minOffset: config.brushMinOffset, canvas);

            var systemMB = UnityEngineUtility.CreateGameObjectWithComponent<GrassTrampleSystemUpdaterMB>();
            systemMB.Init(config, tracker, renderer);

            return new GrassTrampleSystemHandle(
                previewTexture,
                tracker,
                cleanupCallback: () =>
                {
                    UnityEngineUtility.DestroyObject(systemMB.gameObject);
                    renderer.Dispose();
                    // GrassTrampleRenderer.DestroyRenderTexture(renderTexture);
                });
        }
    }
}

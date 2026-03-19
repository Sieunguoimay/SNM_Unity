using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemInstaller
    {
        public GrassTrampleSystemHandle Install(
            GrassTrampleSystemConfig config,
            int textureSize,
            SurfaceCanvas canvas)
        {
            var renderTexture = GrassTrampleRenderer.CreateRenderTexture(textureSize);

            var pingPong = new PingPongTexture(renderTexture.descriptor);
            var material = new UnityEngine.Material(config.shader);
            var stampRenderer = new SurfaceStampRenderer(material, pingPong);
            var stampBuffer = new StampBuffer(64);

            var renderer = new GrassTrampleRenderer(stampRenderer, stampBuffer, canvas, config.fadeSpeed);

            var tracker = new GrassDisturberTracker(minOffset: config.brushMinOffset, canvas);

            var systemMB = UnityEngineUtility.CreateGameObjectWithComponent<GrassTrampleSystemUpdaterMB>();
            systemMB.Init(config, tracker, renderer);

            return new GrassTrampleSystemHandle(
                renderTexture,
                tracker,
                cleanupCallback: () =>
                {
                    UnityEngineUtility.DestroyObject(systemMB.gameObject);
                    renderer.Dispose();
                    GrassTrampleRenderer.DestroyRenderTexture(renderTexture);
                });
        }
    }
}

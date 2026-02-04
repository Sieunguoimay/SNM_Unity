using Snm.Runtime.Unity;

namespace Snm.Runtime.GrassSystem
{

    public class GrassTrampleSystemInstaller
    {
        public GrassTrampleSystemHandle Install(
            GrassTrampleSystemConfig config,
            int textureSize,
            WorldCanvas worldCanvas)
        {
            var renderTexture = GrassTrampleRenderer.CreateRenderTexture(textureSize);
            var renderer = new GrassTrampleRenderer(config.shader, renderTexture, worldCanvas, config.fadeSpeed);

            var brushRegistry = new GrassTrampleBrushRegistry();
            var brushBatchMaker = new BrushRenderBatchesMaker(renderer, brushRegistry, brushesPerBatch: 64);
            var brushDirUpdater = new GrassTrampleBrushDirUpdater(brushRegistry, minOffset: config.brushMinOffset, new(worldCanvas));

            var systemMB = UnityEngineUtility.CreateGameObjectWithComponent<GrassTrampleSystemUpdaterMB>();

            systemMB.SetBrushDirUpdater(brushDirUpdater);
            systemMB.SetBrushBatchMaker(brushBatchMaker);
            systemMB.SetRenderer(renderer);

            return new GrassTrampleSystemHandle(
                renderTexture,
                cleanupCallback: () =>
                {
                    UnityEngineUtility.DestroyObject(systemMB.gameObject);
                    renderer.Cleanup();
                    GrassTrampleRenderer.DestroyRenderTexture(renderTexture);
                }, brushRegistry);
        }
    }
}
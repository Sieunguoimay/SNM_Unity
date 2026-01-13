using Snm.PropertyAttributes;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemInstaller
    {
        public const string RequiredShader_InteractiveGrass = "Snm/InteractiveGrass";

        public GrassSystemHandle Install(GrassSystemConfig systemConfig)
        {
            RequireShaderAttribute.CheckValid(systemConfig.grassMaterial, RequiredShader_InteractiveGrass);

            var grassField = Object.Instantiate(systemConfig.grassFieldPrefab);
            var grassRenderer = new GrassFieldRenderer(systemConfig.grassMesh, systemConfig.grassMaterial);
            var worldCanvas = grassField.GetWorldCanvas();
            var trampleSystemHandle = new GrassTrampleSystemInstaller().Install(systemConfig.trampleSystemConfig, grassField.Dimension.x, worldCanvas);

            var debugManager = new GrassDebugWindowInstaller()
                .Install(() => new GrassDebugTool(worldCanvas, systemConfig, trampleSystemHandle.GetTrampleTexture()));

            grassRenderer.SetMatrices(grassField.GetGrassMatrices());
            grassRenderer.SetWorldCanvas(grassField.GetWorldCanvas());
            grassRenderer.SetWorldBounds(grassField.GetWorldBounds(1, 1));

            grassRenderer.SetWindConfig(systemConfig.windConfig);
            grassRenderer.SetTrampleConfig(trampleSystemHandle.GetTrampleTexture(), systemConfig.trampleConfig);

            var rendererMB = new GameObject("[GrassFieldRendenderMB]").AddComponent<GrassFieldRendererMB>();
            rendererMB.SetRenderer(grassRenderer);

            var brushMBs = grassField.GetComponentsInChildren<GrassTrampleBrushMB>();
            foreach (var brushMB in brushMBs) trampleSystemHandle.BrushRegistry.Register(brushMB.Brush);

            var manager = new GrassSystemHandle(destroyCallback: () =>
            {
                foreach (var brushMB in brushMBs) trampleSystemHandle.BrushRegistry.Unregister(brushMB.Brush);

                trampleSystemHandle.Cleanup();
                grassRenderer.Cleanup();
                Object.DestroyImmediate(grassField.gameObject);
                Object.DestroyImmediate(rendererMB.gameObject);
                debugManager.Cleanup();
            },
            openDebugToolCallback: debugManager.OpenWindow);

            return manager;
        }
    }
}
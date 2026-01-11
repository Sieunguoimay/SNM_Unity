using Snm.PropertyAttributes;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemInstaller
    {
        public const string RequiredShader_InteractiveGrass = "Snm/InteractiveGrass";

        public GrassSystemManager Install(GrassSystemConfig systemConfig)
        {
            RequireShaderAttribute.CheckValid(systemConfig.grassMaterial, RequiredShader_InteractiveGrass);

            var grassField = Object.Instantiate(systemConfig.grassFieldPrefab);
            var grassRenderer = new GrassFieldRenderer(systemConfig.grassMesh, systemConfig.grassMaterial);
            var worldCanvas = grassField.GetWorldCanvas();
            var grassTrampleSystemHandle = new GrassTrampleSystemInstaller().Install();

            var debugManager = new GrassDebugWindowInstaller()
                .Install(() => new GrassDebugTool(worldCanvas, systemConfig));

            grassRenderer.SetMatrices(grassField.GetGrassMatrices());
            grassRenderer.SetWorldCanvas(grassField.GetWorldCanvas());
            grassRenderer.SetWorldBounds(grassField.GetWorldBounds(1, 1));

            grassRenderer.SetWindConfig(systemConfig.windConfig);
            grassRenderer.SetTrampleConfig(grassTrampleSystemHandle.GetTrampleTexture(), systemConfig.trampleConfig);

            var rendererMB = new GameObject("[GrassFieldRendenderMB]").AddComponent<GrassFieldRendererMB>();
            rendererMB.SetRenderer(grassRenderer);

            var manager = new GrassSystemManager(destroyCallback: () =>
            {
                grassTrampleSystemHandle.Cleanup();
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
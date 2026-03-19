using Snm.PropertyAttributes;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemInstaller
    {
        public const string RequiredShader_InteractiveGrass = "Snm/InteractiveGrass";

        public GrassSystemHandle Install(GrassSystemConfig systemConfig, GrassField grassField)
        {
            RequireShaderAttribute.CheckValid(systemConfig.grassMaterial, RequiredShader_InteractiveGrass);

            var shouldDestroyGrassField = grassField == null;
            grassField ??= Object.Instantiate(systemConfig.grassFieldPrefab);

            var canvas = grassField.GetSurfaceCanvas();

            var trampleSystemHandle = new GrassTrampleSystemInstaller().Install(
                systemConfig.trampleSystemConfig,
                grassField.Dimension.x,
                canvas);
            var trampleMap = trampleSystemHandle.GetTrampleTexture();

#if UNITY_EDITOR
            var debugManager = new GrassDebugWindowInstaller()
                .Install(() => new GrassDebugTool(canvas, systemConfig, trampleMap));
#endif
            var grassRenderer = new GrassFieldRenderer(systemConfig.grassMesh, systemConfig.grassMaterial);
            grassRenderer.SetMatrices(grassField.GetGrassMatrices());
            grassRenderer.SetWorldCanvas(canvas);
            grassRenderer.SetWorldBounds(grassField.GetWorldBounds(1, 1));
            grassRenderer.SetWindConfig(systemConfig.windConfig);
            grassRenderer.SetTrampleMap(trampleMap);

            var rendererMB = UnityEngineUtility.CreateGameObjectWithComponent<GrassFieldRendererMB>();
            rendererMB.SetRenderer(grassRenderer);

            var brushMBs = grassField.GetComponentsInChildren<GrassTrampleBrushMB>(true);
            foreach (var brushMB in brushMBs) trampleSystemHandle.RegisterLocal(brushMB);

            var manager = new GrassSystemHandle(
                trampleSystemHandle,
                destroyCallback: () =>
                {
                    trampleSystemHandle.Cleanup();
                    grassRenderer.Cleanup();
                    if (shouldDestroyGrassField) UnityEngineUtility.DestroyObject(grassField.gameObject);
                    UnityEngineUtility.DestroyObject(rendererMB.gameObject);
#if UNITY_EDITOR
                    debugManager.Cleanup();
#endif
                },
                openDebugToolCallback: () =>
                {
#if UNITY_EDITOR
                    debugManager.OpenWindow();
#endif
                });

            return manager;
        }
    }
}

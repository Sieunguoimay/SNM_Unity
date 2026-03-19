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

            var grassRenderer = new GrassFieldRenderer(systemConfig.grassMesh, systemConfig.grassMaterial);
            grassRenderer.SetMatrices(grassField.GetGrassMatrices());
            grassRenderer.SetWorldCanvas(canvas);
            grassRenderer.SetWorldBounds(grassField.GetWorldBounds(1, 1));
            grassRenderer.SetWindConfig(systemConfig.windConfig);
            grassRenderer.SetTrampleMap(trampleMap);

            var rendererMB = UnityEngineUtility.CreateGameObjectWithComponent<GrassFieldRendererMB>();
            rendererMB.SetRenderer(grassRenderer);

            var instanceCount = grassField.Dimension.x * grassField.Dimension.y;

            var manager = new GrassSystemHandle(
                trampleSystemHandle,
                destroyCallback: () =>
                {
                    trampleSystemHandle.Cleanup();
                    grassRenderer.Cleanup();
                    if (shouldDestroyGrassField) UnityEngineUtility.DestroyObject(grassField.gameObject);
                    UnityEngineUtility.DestroyObject(rendererMB.gameObject);
                },
                config: systemConfig,
                grassField: grassField,
                canvas: canvas,
                tracker: trampleSystemHandle.Tracker,
                instanceCount: instanceCount);

            return manager;
        }
    }
}

using System;
using Snm.PropertyAttributes;

namespace Snm.Runtime.GrassSystem
{

    public class GrassSystemManager
    {
        private readonly Action destroyCallback;

        public GrassSystemManager(Action destroyCallback)
        {
            this.destroyCallback = destroyCallback;
        }

        public void DestroySystem()
        {
            destroyCallback?.Invoke();
        }
    }

    public class GrassSystemInstaller
    {
        public const string RequiredShader_InteractiveGrass = "Snm/InteractiveGrass";

        public GrassSystemManager Install(GrassSystemConfig systemConfig)
        {
            RequireShaderAttribute.CheckValid(systemConfig.grassMaterial, RequiredShader_InteractiveGrass);

            var grassField = UnityEngine.Object.Instantiate(systemConfig.grassFieldPrefab);
            var grassRenderer = new GrassFieldRenderer(systemConfig.grassMesh, systemConfig.grassMaterial);

            grassRenderer.SetMatrices(grassField.GetGrassWorldMatrices());
            grassRenderer.SetWindConfig(systemConfig.windConfig);
            // grassRenderer.SetWorldCanvas(new WorldCanvas{worldMin = });

            grassField.SetRenderer(grassRenderer);

            var manager = new GrassSystemManager(destroyCallback: () =>
            {
                grassRenderer.Cleanup();
                UnityEngine.Object.DestroyImmediate(grassField.gameObject);
            });

            return manager;
        }
    }
}

// ─────────────────────────────────────────────
// ReflectionInstaller.cs
// Wires the reflection feature together.
// ─────────────────────────────────────────────
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionInstaller
    {
        public static ReflectionHandle Install(WaterFeatureContext ctx)
        {
            var reflectionCamera = new ReflectionCamera(ctx.SourceCamera);
            var renderTexture = CreateRenderTexture(ctx.Config, reflectionCamera.Camera);

            var renderer = new ReflectionRenderer(
                reflectionCamera.Camera,
                renderTexture);

            var controller = new ReflectionController(
                ctx.SourceCamera,
                reflectionCamera,
                ctx.Surface,
                renderer,
                ctx.SurfaceMaterial,
                renderTexture,
                frameInterval: 4,
                ctx.UpdateService);

            return new ReflectionHandle(reflectionCamera, renderTexture, controller);
        }

        private static RenderTexture CreateRenderTexture(WaterConfig config, Camera reflectionCamera)
        {
            int width = config.reflectionTextureWidth;
            int height = Mathf.CeilToInt(width / reflectionCamera.aspect);
            return new RenderTexture(width, height, depth: 16);
        }
    }
}

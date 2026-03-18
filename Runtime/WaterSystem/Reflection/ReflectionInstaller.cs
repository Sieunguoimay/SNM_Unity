
// ─────────────────────────────────────────────
// ReflectionInstaller.cs
// Wires the reflection feature together.
// ─────────────────────────────────────────────
using Snm.DependencyInjection;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public static class ReflectionInstaller
    {
        private static readonly int ReflectionTexID = Shader.PropertyToID("_ReflectionTex");

        public static ReflectionFeature Install(WaterFeatureContext ctx)
        {
            var reflectionCamera = new ReflectionCamera(ctx.SourceCamera);
            var renderTexture = CreateRenderTexture(ctx.Config, reflectionCamera.Camera);

            var renderer = new ReflectionRenderer(
                reflectionCamera.Camera,
                renderTexture);

            var state = new ReflectionState();
            var tracker = new CameraTracker(ctx.SourceCamera);
            var shaderBinder = new ReflectionShaderBinder(ctx.SurfaceMaterial, reflectionCamera);
            var scheduler = new ReflectionRenderScheduler(frameInterval: 4);

            // Set reflection texture once — it does not change.
            ctx.SurfaceMaterial.SetTexture(ReflectionTexID, renderTexture);

            var feature = new ReflectionFeature(
                ctx.SourceCamera,
                tracker,
                reflectionCamera,
                renderTexture,
                ctx.Surface,
                renderer,
                shaderBinder,
                scheduler,
                state);

            return feature;
        }

        private static RenderTexture CreateRenderTexture(WaterConfig config, Camera reflectionCamera)
        {
            int width = config.reflection.textureWidth;
            int height = Mathf.CeilToInt(width / reflectionCamera.aspect);
            return new RenderTexture(width, height, depth: 16);
        }
    }
}

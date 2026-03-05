// ─────────────────────────────────────────────
// WaterReflectionRenderer.cs
// Renders the reflection camera and fires a callback when done.
// ─────────────────────────────────────────────
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionRenderer
    {
        private readonly Camera camera;
        private readonly RenderTexture target;

        public ReflectionRenderer(
            Camera camera,
            RenderTexture target)
        {
            this.camera = camera;
            this.target = target;
        }

        public void Render(Matrix4x4 projection)
        {
            // Swap in projection, render, restore.
            var prevProjection = camera.projectionMatrix;
            var prevTarget = camera.targetTexture;

            camera.projectionMatrix = projection;
            camera.targetTexture = target;
            camera.Render();

            camera.projectionMatrix = prevProjection;
            camera.targetTexture = prevTarget;
        }
    }
}
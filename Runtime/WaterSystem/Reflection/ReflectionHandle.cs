// ─────────────────────────────────────────────
// ReflectionHandle.cs
// Lifetime wrapper for the reflection feature.
// ─────────────────────────────────────────────
using System;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionHandle : IDisposable
    {
        private readonly ReflectionCamera reflectionCamera;
        private readonly RenderTexture renderTexture;
        private readonly ReflectionController controller;

        public ReflectionHandle(
            ReflectionCamera reflectionCamera,
            RenderTexture renderTexture,
            ReflectionController controller)
        {
            this.reflectionCamera = reflectionCamera;
            this.renderTexture = renderTexture;
            this.controller = controller;
        }

        public void Dispose()
        {
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
            reflectionCamera.Dispose();
            controller.Dispose();
        }
    }
}

// ─────────────────────────────────────────────
// ReflectionHandle.cs
// Lifetime wrapper for the reflection feature.
// ─────────────────────────────────────────────
using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionHandle : IDisposable
    {
        private readonly ReflectionCamera reflectionCamera;
        private readonly RenderTexture renderTexture;
        private readonly ReflectionFeature feature;
        private bool _isDisposed = false;

        public RenderTexture Texture => renderTexture;

        public ReflectionHandle(
            ReflectionCamera reflectionCamera,
            RenderTexture renderTexture,
            ReflectionFeature feature)
        {
            this.reflectionCamera = reflectionCamera;
            this.renderTexture = renderTexture;
            this.feature = feature;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            feature.Dispose();
            renderTexture.Release();
            UnityEngineUtility.DestroyObject(renderTexture);
            reflectionCamera.Dispose();
        }
    }
}

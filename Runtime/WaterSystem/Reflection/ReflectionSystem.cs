using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionSystem : IDisposable
    {
        private readonly Action disposeCallback;

        public RenderTexture ReflectionRT { get; }
        public PreviewReflectionTexture PreviewReflectionTexture { get; }

        public Action<Matrix4x4> OnReflectionVPChanged;

        public ReflectionSystem(
            Action disposeCallback,
            RenderTexture reflectionRT,
            ReflectionMatrixData reflectionMatrixData,
            PreviewReflectionTexture previewReflectionTexture,
            ReflectionMatrixDataUpdater reflectionMatrixDataUpdater)
        {
            this.disposeCallback = disposeCallback;

            ReflectionRT = reflectionRT;
            PreviewReflectionTexture = previewReflectionTexture;

            reflectionMatrixDataUpdater.SetCallback(() => OnReflectionVPChanged?.Invoke(reflectionMatrixData.VP));
        }

        public void Dispose()
        {
            disposeCallback?.Invoke();
        }
    }
}
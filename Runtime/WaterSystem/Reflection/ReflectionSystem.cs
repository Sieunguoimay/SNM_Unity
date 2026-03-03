using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionSystem : IDisposable
    {
        private readonly Action disposeCallback;
        public RenderTexture reflectionRT;
        public Camera reflectionCamera;
        public ReflectionMatrixData reflectionMatrixData;
        public TransformChangeDetector targetCamMoveDetector;
        public WaterReflectionRenderController reflectionRenderController;
        public PreviewReflectionTexture previewReflectionTexture;
        public TransformReflectionMover reflectionCameraMover;

        public ReflectionSystem(
            Action disposeCallback,
            RenderTexture reflectionRT,
            Camera reflectionCamera,
            ReflectionMatrixData reflectionMatrixData,
            TransformChangeDetector targetCamMoveDetector,
            WaterReflectionRenderController reflectionRenderController,
            PreviewReflectionTexture previewReflectionTexture,
            TransformReflectionMover reflectionCameraMover)
        {
            this.disposeCallback = disposeCallback;
            this.reflectionRT = reflectionRT;
            this.reflectionCamera = reflectionCamera;
            this.reflectionMatrixData = reflectionMatrixData;
            this.targetCamMoveDetector = targetCamMoveDetector;
            this.reflectionRenderController = reflectionRenderController;
            this.previewReflectionTexture = previewReflectionTexture;
            this.reflectionCameraMover = reflectionCameraMover;
        }

        public void Dispose()
        {
            disposeCallback?.Invoke();
        }
    }
}
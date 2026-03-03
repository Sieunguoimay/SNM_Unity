using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{

    public class WaterReflectionRenderController : ILateUpdateTarget
    {
        private readonly ReflectionCameraRenderer renderer;
        private readonly int interval;
        private bool _dirty;

        public WaterReflectionRenderController(
            ReflectionCameraRenderer renderer,
            int interval)
        {
            this.renderer = renderer;
            this.interval = interval;
        }

        public void LateUpdate()
        {
            if (ShouldRender())
            {
                renderer.Render();
                _dirty = false;
            }
        }

        private bool ShouldRender()
        {
            if (_dirty) return true;
            if (interval <= 1) return true;
            if (Time.frameCount % interval == 0) return true;
            return false;
        }

        public void MarkDirty()
        {
            _dirty = true;
        }
    }

    public class ReflectionCameraRenderer
    {
        private readonly ReflectionMatrixData reflectionData;
        private readonly Camera reflectionCamera;
        private readonly RenderTexture renderTexture;
        private readonly ICameraRenderExecutor renderExecutor;
        private readonly Action textureChangeCallback;

        public ReflectionCameraRenderer(
            ReflectionMatrixData reflectionData,
            Camera reflectionCamera,
            RenderTexture renderTexture,
            ICameraRenderExecutor renderExecutor,
            Action textureChangeCallback)
        {
            this.reflectionData = reflectionData;
            this.reflectionCamera = reflectionCamera;
            this.renderTexture = renderTexture;
            this.renderExecutor = renderExecutor;
            this.textureChangeCallback = textureChangeCallback;
        }


        public void Render()
        {
            renderExecutor.Render(reflectionCamera, renderTexture, reflectionData.Proj);
            textureChangeCallback?.Invoke();
        }
    }

    public interface ICameraRenderExecutor
    {
        void Render(Camera cam, RenderTexture rt, Matrix4x4 proj);
    }

    public class DefaultCameraRenderExecutor : ICameraRenderExecutor
    {
        public void Render(Camera cam, RenderTexture rt, Matrix4x4 proj)
            => RenderToTexture(cam, rt, proj);

        public static void RenderToTexture(Camera cam, RenderTexture rt, Matrix4x4 proj)
        {
            var prevProj = cam.projectionMatrix;
            var prevTexture = cam.targetTexture;

            cam.projectionMatrix = proj;
            cam.targetTexture = rt;
            cam.Render();
            cam.projectionMatrix = prevProj;
            cam.targetTexture = prevTexture;
        }
    }
}
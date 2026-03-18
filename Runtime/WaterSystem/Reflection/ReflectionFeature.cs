using System;
using Snm.Reactivity;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionFeature : IWaterFeature
    {
        private readonly ReflectionCamera _reflectionCamera;
        private readonly RenderTexture _renderTexture;
        private readonly CameraTracker _tracker;
        private readonly ReflectionRenderScheduler _scheduler;
        private readonly ReflectionRenderer _renderer;
        private readonly ReflectionState _state;

        private readonly Effect _pipelineEffect;
        private readonly Effect _shaderBindEffect;
        private bool _isDisposed;

        public RenderTexture Texture => _renderTexture;

        public ReflectionFeature(
            Camera sourceCamera,
            CameraTracker tracker,
            ReflectionCamera reflectionCamera,
            RenderTexture renderTexture,
            SurfaceCanvas surface,
            ReflectionRenderer renderer,
            ReflectionShaderBinder shaderBinder,
            ReflectionRenderScheduler scheduler,
            ReflectionState state)
        {
            _reflectionCamera = reflectionCamera;
            _renderTexture = renderTexture;
            _tracker = tracker;
            _scheduler = scheduler;
            _renderer = renderer;
            _state = state;

            // Effect 1: camera move → mirror transform + compute projection
            _pipelineEffect = new Effect(() =>
            {
                _ = tracker.Position.Value;
                _ = tracker.Rotation.Value;

                var plane = new ReflectionPlane(surface);

                reflectionCamera.MirrorAcross(sourceCamera.transform, in plane);
                reflectionCamera.SyncLensSettings(sourceCamera);

                state.Projection.Value = ReflectionProjection.Compute(
                    reflectionCamera.Camera,
                    state.ComputeWaterCorners(surface),
                    in plane);
            });

            // Effect 2: projection changed → bind shader + request render
            _shaderBindEffect = new Effect(() =>
            {
                var projection = state.Projection.Value;

                shaderBinder.Bind(projection);

                state.RenderRequested.Value = true;
            });
        }

        public void OnUpdate(float deltaTime)
        {
            _tracker.Poll();
        }

        public void OnLateUpdate()
        {
            if (!_scheduler.ShouldRender(_state.RenderRequested.Value)) return;

            _renderer.Render(_state.Projection.Value);
            _state.RenderRequested.Value = false;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _pipelineEffect.Dispose();
            _shaderBindEffect.Dispose();
            _renderTexture.Release();
            UnityEngineUtility.DestroyObject(_renderTexture);
            _reflectionCamera.Dispose();
        }
    }
}

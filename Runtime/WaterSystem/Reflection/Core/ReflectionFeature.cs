using System;
using Snm.Reactivity;
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionFeature : IUpdateTarget, ILateUpdateTarget, IDisposable
    {
        private readonly CameraTracker _tracker;
        private readonly ReflectionRenderScheduler _scheduler;
        private readonly ReflectionRenderer _renderer;
        private readonly ReflectionState _state;
        private readonly IUpdateService _updateService;

        private readonly Effect _pipelineEffect;
        private readonly Effect _shaderBindEffect;

        public ReflectionFeature(
            Camera sourceCamera,
            CameraTracker tracker,
            ReflectionCamera reflectionCamera,
            SurfaceData surface,
            ReflectionRenderer renderer,
            ReflectionShaderBinder shaderBinder,
            ReflectionRenderScheduler scheduler,
            ReflectionState state,
            IUpdateService updateService)
        {
            _tracker = tracker;
            _scheduler = scheduler;
            _renderer = renderer;
            _state = state;
            _updateService = updateService;

            // Effect 1: camera move → mirror transform + compute projection
            _pipelineEffect = new Effect(() =>
            {
                _ = tracker.Position.Value;
                _ = tracker.Rotation.Value;

                var plane = new ReflectionPlane(surface);

                reflectionCamera.MirrorAcross(sourceCamera.transform, in plane);

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

            updateService.AddUpdateTarget(this);
            updateService.AddLateUpdateTarget(this);
        }

        public void Update(float deltaTime)
        {
            _tracker.Poll();
        }

        public void LateUpdate()
        {
            if (!_scheduler.ShouldRender(_state.RenderRequested.Value)) return;

            _renderer.Render(_state.Projection.Value);
            _state.RenderRequested.Value = false;
        }

        public void Dispose()
        {
            _pipelineEffect.Dispose();
            _shaderBindEffect.Dispose();
            _updateService.RemoveUpdateTarget(this);
            _updateService.RemoveLateUpdateTarget(this);
        }
    }
}


// ─────────────────────────────────────────────
// WaterReflectionController.cs
// Owns the per-frame update logic:
//   - detects camera movement
//   - mirrors camera across water plane
//   - recomputes projection
//   - schedules renders
//   - writes shader properties directly to the surface material
// ─────────────────────────────────────────────
using System;
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionController : IDisposable, IUpdateTarget, ILateUpdateTarget
    {
        private static readonly int ReflectionTexID = Shader.PropertyToID("_ReflectionTex");
        private static readonly int ReflectionVPID  = Shader.PropertyToID("_ReflectionVP");

        private readonly Camera sourceCamera;
        private readonly ReflectionCamera reflectionCamera;
        private readonly SurfaceData waterSurface;
        private readonly ReflectionRenderer renderer;
        private readonly Material material;
        private readonly int frameInterval;
        private readonly IUpdateService updaterSerivce;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Matrix4x4 _currentProjection;
        private bool _isDirty;

        private const float MinMoveDelta = 0.01f;
        private const float MinAngleDelta = 0.1f;

        public ReflectionController(
            Camera sourceCamera,
            ReflectionCamera reflectionCamera,
            SurfaceData waterSurface,
            ReflectionRenderer renderer,
            Material material,
            RenderTexture reflectionTexture,
            int frameInterval,
            IUpdateService updaterSerivce)
        {
            this.sourceCamera = sourceCamera;
            this.reflectionCamera = reflectionCamera;
            this.waterSurface = waterSurface;
            this.renderer = renderer;
            this.material = material;
            this.frameInterval = frameInterval;
            this.updaterSerivce = updaterSerivce;

            // Set reflection texture once — it does not change.
            material.SetTexture(ReflectionTexID, reflectionTexture);

            // Force a full sync on the first frame.
            SyncReflectionCamera();
            _isDirty = true;

            updaterSerivce.AddUpdateTarget(this);
            updaterSerivce.AddLateUpdateTarget(this);
        }

        public void Dispose()
        {
            updaterSerivce.RemoveUpdateTarget(this);
            updaterSerivce.RemoveLateUpdateTarget(this);
        }

        // Called every Update — sync camera mirror and projection if camera moved.
        public void Update()
        {
            if (!SourceCameraMoved()) return;

            SyncReflectionCamera();
            _isDirty = true;
        }

        // Called every LateUpdate — render when needed.
        public void LateUpdate()
        {
            if (!ShouldRenderThisFrame()) return;

            renderer.Render(_currentProjection);
            _isDirty = false;
        }

        // ── private helpers ───────────────────────────────────────────────────

        private void SyncReflectionCamera()
        {
            var plane = new ReflectionPlane(waterSurface);

            reflectionCamera.MirrorAcross(sourceCamera.transform, in plane);

            UpdateProjectionMatrix(in plane);
        }

        private void UpdateProjectionMatrix(in ReflectionPlane plane)
        {
            _currentProjection = ReflectionProjection.Compute(
                reflectionCamera.Camera,
                GetWaterCorners(waterSurface),
                in plane);

            // Write the VP matrix directly to the surface material.
            var vp = _currentProjection * reflectionCamera.Camera.worldToCameraMatrix;
            material.SetMatrix(ReflectionVPID, vp);
        }

        private bool SourceCameraMoved()
        {
            sourceCamera.transform.GetPositionAndRotation(out var pos, out var rot);

            bool moved = (pos - _lastPosition).sqrMagnitude > MinMoveDelta * MinMoveDelta;
            bool rotated = Quaternion.Angle(_lastRotation, rot) > MinAngleDelta;

            if (moved || rotated)
            {
                _lastPosition = pos;
                _lastRotation = rot;
                return true;
            }
            return false;
        }

        private bool ShouldRenderThisFrame()
        {
            if (_isDirty) return true;
            if (frameInterval <= 1) return true;
            if (Time.frameCount % frameInterval == 0) return true;
            return false;
        }

        private static Vector3[] GetWaterCorners(SurfaceData water)
        {
            var right = water.rotation * Vector3.right * water.size.x * 0.5f;
            var forward = water.rotation * Vector3.forward * water.size.y * 0.5f;
            var center = water.position;
            return new[]
            {
                center - right - forward,
                center - right + forward,
                center + right + forward,
                center + right - forward,
            };
        }
    }
}

using System;
using System.Collections.Generic;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraSystem : IFixedUpdateTarget, IDisposable
    {
        private readonly Camera camera;
        private readonly CameraRigConfig baseConfig;
        private readonly IUpdateService updateService;

        private readonly List<CameraTarget> targets = new();
        private readonly CameraShakeState shakeState = new();
        private readonly CameraSequencePlayer sequencePlayer = new();
        private CameraRigVisualizerMB visualizer;
        private CameraRigVisualizeData visualizeData;

        // Look-ahead state
        private Vector2 currentLookAheadBias;

        // Config blending state
        private CameraRigConfig activeConfig;
        private CameraRigConfig fromConfig;
        private CameraRigConfig targetConfig;
        private float configBlendElapsed;
        private float configBlendDuration;
        private bool configBlending;

        // Manual zoom state
        private float manualZoomOffset;
        private float targetManualZoomOffset;

        // Pitch state
        private float currentPitch;
        private float? pitchOverride;

        // Screen constraint state
        private Vector2 lastScreenSize;

        // Multi-camera blend state
        private Camera externalCamera;
        private float cameraBlendWeight;
        private float cameraBlendTarget;
        private float cameraBlendSpeed;

        public CameraSystem(Camera camera, CameraRigConfig config, IUpdateService updateService)
        {
            this.camera = camera;
            this.baseConfig = config;
            this.updateService = updateService;

            // Active config is a mutable copy used for blending
            activeConfig = config;
            currentPitch = config.defaultPitch;

            if (config.debugVisualize)
            {
                visualizeData = new CameraRigVisualizeData
                {
                    camera = camera,
                    targets = targets,
                };
                visualizer = UnityEngineUtility.CreateGameObjectWithComponent<CameraRigVisualizerMB>();
                visualizer.SetVisualizeData(visualizeData);
            }

            updateService.AddFixedUpdateTarget(this, UpdatePriority.Camera);
        }

        public void Dispose()
        {
            updateService.RemoveFixedUpdateTarget(this);
            if (visualizer != null)
                UnityEngine.Object.Destroy(visualizer.gameObject);
        }

        // ── Position ─────────────────────────────────────────────

        public Vector3 Position => camera.transform.position;

        // ── Targets ──────────────────────────────────────────────

        public IReadOnlyList<CameraTarget> Targets => targets;

        public void Register(CameraTarget target) => targets.Add(target);
        public void Unregister(CameraTarget target) => targets.Remove(target);

        // ── Shake ────────────────────────────────────────────────

        public void Shake(float intensity, float duration, float frequency = 25f)
        {
            shakeState.AddShake(intensity, duration, frequency);
        }

        // ── Focus / Sequence ─────────────────────────────────────

        public void FocusOn(Vector3 point, float blendIn = 0.3f, float hold = 1f, float blendOut = 0.3f)
        {
            PlaySequence(new CameraSequence()
                .FocusOn(point, blendIn, hold)
                .Return(blendOut));
        }

        public void PlaySequence(CameraSequence sequence) => sequencePlayer.Play(sequence);
        public void CancelSequence() => sequencePlayer.Cancel();
        public bool IsPlayingSequence => sequencePlayer.IsPlaying;

        // ── Zone Config Switching ────────────────────────────────

        public void TransitionTo(CameraRigConfig toConfig, float duration = 1f)
        {
            if (duration <= 0f)
            {
                activeConfig = toConfig;
                configBlending = false;
                return;
            }

            fromConfig = activeConfig;
            targetConfig = toConfig;
            activeConfig = new CameraRigConfig();
            configBlendElapsed = 0f;
            configBlendDuration = duration;
            configBlending = true;
        }

        public void TransitionToDefault(float duration = 0.5f) => TransitionTo(baseConfig, duration);

        // ── Manual Zoom ──────────────────────────────────────────

        public void SetZoomInput(float delta)
        {
            targetManualZoomOffset = Mathf.Clamp(
                targetManualZoomOffset + delta * activeConfig.zoomSensitivity,
                0f,
                activeConfig.maxDistance - activeConfig.minDistance);
        }

        public void SetZoomLevel(float normalized01)
        {
            targetManualZoomOffset = Mathf.Lerp(0f,
                activeConfig.maxDistance - activeConfig.minDistance, Mathf.Clamp01(normalized01));
        }

        // ── Pitch ────────────────────────────────────────────────

        public void SetPitchOverride(float degrees)
        {
            pitchOverride = Mathf.Clamp(degrees, 10f, 89f);
        }

        public void ClearPitchOverride() => pitchOverride = null;

        // ── Multi-Camera Blend ───────────────────────────────────

        public void BlendToCamera(Camera target, float duration = 0.5f)
        {
            externalCamera = target;
            cameraBlendTarget = 1f;
            cameraBlendSpeed = duration > 0f ? 1f / duration : float.MaxValue;
        }

        public void BlendBack(float duration = 0.5f)
        {
            cameraBlendTarget = 0f;
            cameraBlendSpeed = duration > 0f ? 1f / duration : float.MaxValue;
        }

        // ── Update ───────────────────────────────────────────────

        void IFixedUpdateTarget.FixedUpdate(float fixedDeltaTime)
        {
            if (targets.Count == 0) return;

            var dt = fixedDeltaTime;
            var cfg = activeConfig;

            // Config blending
            if (configBlending)
            {
                configBlendElapsed += dt;
                var t = Mathf.Clamp01(configBlendElapsed / configBlendDuration);
                CameraConfigLerp.Lerp(fromConfig, targetConfig, t, activeConfig);
                if (t >= 1f)
                {
                    activeConfig = targetConfig;
                    configBlending = false;
                }
                cfg = activeConfig;
            }

            // Skip internal calculations if external camera fully owns output
            if (externalCamera != null && cameraBlendWeight >= 0.999f)
            {
                AdvanceExternalBlend(dt);
                return;
            }

            var vp = camera.projectionMatrix * camera.worldToCameraMatrix;
            CombineTargets(vp, out var combinedDir, out var ndcBounds, out var avgVelocity, out var avgWorldPos);

            // Dead zone: reduce NDC center offset
            if (cfg.deadZoneX > 0f || cfg.deadZoneY > 0f)
            {
                var center = ndcBounds.center;
                center.x = ApplyDeadZone(center.x, cfg.deadZoneX);
                center.y = ApplyDeadZone(center.y, cfg.deadZoneY);
                ndcBounds = new Bounds(center, ndcBounds.size);
            }

            // Look-ahead: convert world velocity to NDC-scale bias, smoothed
            {
                var targetBias = Vector2.zero;

                if (cfg.lookAheadFactor > 0f && avgVelocity.sqrMagnitude > 0.001f)
                {
                    var camRight = camera.transform.right;
                    var camUp = camera.transform.up;
                    var velX = Vector3.Dot(avgVelocity, camRight);
                    var velY = Vector3.Dot(avgVelocity, camUp);

                    var viewDepth = Mathf.Max(0.1f,
                        Vector3.Dot(avgWorldPos - camera.transform.position, camera.transform.forward));
                    var halfFovTan = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

                    targetBias = new Vector2(
                        velX / (viewDepth * halfFovTan * camera.aspect),
                        velY / (viewDepth * halfFovTan)) * cfg.lookAheadFactor;

                    if (targetBias.magnitude > cfg.lookAheadMaxOffset)
                        targetBias = targetBias.normalized * cfg.lookAheadMaxOffset;
                }

                currentLookAheadBias = Vector2.Lerp(currentLookAheadBias, targetBias, cfg.convergenceRateXY);

                if (currentLookAheadBias.sqrMagnitude > 0.0001f)
                {
                    var center = ndcBounds.center;
                    center.x += currentLookAheadBias.x;
                    center.y += currentLookAheadBias.y;
                    ndcBounds = new Bounds(center, ndcBounds.size);
                }
            }

            // Padding
            if (cfg.ndcPaddingX > 0f || cfg.ndcPaddingY > 0f)
                ndcBounds.Expand(new Vector3(cfg.ndcPaddingX * 2f, cfg.ndcPaddingY * 2f, 0f));

            // Pitch + Rotation
            {
                var targetPitch = pitchOverride ?? cfg.defaultPitch;
                currentPitch = Mathf.Lerp(currentPitch, targetPitch, cfg.pitchConvergenceRate);

                if (combinedDir != null)
                {
                    var flatDir = new Vector3(combinedDir.Value.x, 0f, combinedDir.Value.z);
                    if (flatDir.sqrMagnitude > 0.001f)
                    {
                        flatDir.Normalize();
                        var targetRot = Quaternion.LookRotation(flatDir) * Quaternion.Euler(currentPitch, 0f, 0f);
                        camera.transform.rotation = Quaternion.Slerp(
                            camera.transform.rotation, targetRot, cfg.convergenceRateRotation);
                    }
                }
                else
                {
                    var euler = camera.transform.eulerAngles;
                    euler.x = Mathf.LerpAngle(euler.x, currentPitch, cfg.pitchConvergenceRate);
                    camera.transform.eulerAngles = euler;
                }
            }

            // Position offset
            var viewOffset = BoundsUtility.CalculateCamOffset_ToCenterAndFitNdcBounds_Perspective(
                camera.projectionMatrix.inverse,
                camera.fieldOfView,
                camera.aspect,
                ndcBounds,
                cfg.convergenceRateXY,
                cfg.convergenceRateZ,
                cfg.minDistance,
                cfg.maxDistance);

            // Manual zoom
            if (cfg.enableManualZoom)
            {
                manualZoomOffset = Mathf.Lerp(manualZoomOffset, targetManualZoomOffset, cfg.zoomSmoothRate);
                viewOffset.z -= manualZoomOffset;
            }

            camera.transform.position += camera.transform.TransformDirection(viewOffset);

            // Cinematic sequence / Focus override
            if (sequencePlayer.IsPlaying)
            {
                sequencePlayer.Advance(dt);
                var w = sequencePlayer.BlendWeight;
                if (w > 0f)
                {
                    var currentPos = camera.transform.position;
                    var dist = (sequencePlayer.CurrentFocusPoint - currentPos).magnitude;
                    var targetPos = sequencePlayer.CurrentFocusPoint - camera.transform.rotation * Vector3.forward * dist;
                    camera.transform.position = Vector3.Lerp(currentPos, targetPos, w);
                }
            }

            // Camera bounds
            if (cfg.useCameraBounds)
            {
                var pos = camera.transform.position;
                pos.x = Mathf.Clamp(pos.x, cfg.cameraBounds.min.x, cfg.cameraBounds.max.x);
                pos.y = Mathf.Clamp(pos.y, cfg.cameraBounds.min.y, cfg.cameraBounds.max.y);
                pos.z = Mathf.Clamp(pos.z, cfg.cameraBounds.min.z, cfg.cameraBounds.max.z);
                camera.transform.position = pos;
            }

            // Screen constraint
            ApplyScreenConstraint(cfg);

            // Shake
            if (shakeState.HasActiveShakes)
            {
                var shakeOffset = shakeState.Evaluate(dt);
                camera.transform.position += camera.transform.TransformDirection(shakeOffset);
            }

            // Multi-camera blend
            if (externalCamera != null)
                AdvanceExternalBlend(dt);

            // Debug
            if (visualizeData != null)
                visualizeData.ndcBounds = ndcBounds;
        }

        private void AdvanceExternalBlend(float dt)
        {
            cameraBlendWeight = Mathf.MoveTowards(cameraBlendWeight, cameraBlendTarget, cameraBlendSpeed * dt);

            camera.transform.position = Vector3.Lerp(
                camera.transform.position, externalCamera.transform.position, cameraBlendWeight);
            camera.transform.rotation = Quaternion.Slerp(
                camera.transform.rotation, externalCamera.transform.rotation, cameraBlendWeight);
            camera.fieldOfView = Mathf.Lerp(
                camera.fieldOfView, externalCamera.fieldOfView, cameraBlendWeight);

            if (cameraBlendTarget == 0f && cameraBlendWeight <= 0.001f)
            {
                externalCamera = null;
                cameraBlendWeight = 0f;
            }
        }

        private void ApplyScreenConstraint(CameraRigConfig cfg)
        {
            if (cfg.screenConstraintMode == ScreenConstraintMode.None) return;

            var screenW = (float)Screen.width;
            var screenH = (float)Screen.height;
            if (screenW == lastScreenSize.x && screenH == lastScreenSize.y) return;
            lastScreenSize = new Vector2(screenW, screenH);

            switch (cfg.screenConstraintMode)
            {
                case ScreenConstraintMode.FixedWidth:
                    camera.orthographicSize = cfg.constrainedWidth / screenW * screenH;
                    break;

                case ScreenConstraintMode.VisibleArea:
                    var constraintAspect = cfg.constrainedVisibleArea.x / cfg.constrainedVisibleArea.y;
                    var currentAspect = camera.aspect;
                    var targetHeight = currentAspect < constraintAspect
                        ? cfg.constrainedVisibleArea.x / currentAspect
                        : cfg.constrainedVisibleArea.y;
                    var distance = Vector3.Distance(camera.transform.position, cfg.constrainedAreaTarget);
                    var tan = targetHeight / (2f * distance);
                    camera.fieldOfView = 2f * Mathf.Atan(tan) * Mathf.Rad2Deg;
                    break;
            }
        }

        private static float ApplyDeadZone(float value, float deadZone)
        {
            if (Mathf.Abs(value) < deadZone) return 0f;
            return value - Mathf.Sign(value) * deadZone;
        }

        private void CombineTargets(
            Matrix4x4 vp,
            out Vector3? combinedDir,
            out Bounds ndcBounds,
            out Vector3 avgVelocity,
            out Vector3 avgWorldPos)
        {
            var sumDir = Vector3.zero;
            var dirCount = 0;
            var sumVelocity = Vector3.zero;
            var sumWorldPos = Vector3.zero;

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var t in targets)
            {
                if (t.DesiredCamDirection != null)
                {
                    sumDir += t.DesiredCamDirection.Value;
                    dirCount++;
                }

                sumVelocity += t.EffectiveVelocity;
                sumWorldPos += t.VisibleBounds.center;

                var b = BoundsUtility.BoundsWorldToNDC(t.VisibleBounds, vp);
                if (b.min.x < min.x) min.x = b.min.x;
                if (b.min.y < min.y) min.y = b.min.y;
                if (b.min.z < min.z) min.z = b.min.z;
                if (b.max.x > max.x) max.x = b.max.x;
                if (b.max.y > max.y) max.y = b.max.y;
                if (b.max.z > max.z) max.z = b.max.z;
            }

            ndcBounds = new Bounds((min + max) / 2f, max - min);
            combinedDir = dirCount > 0 ? (sumDir / dirCount).normalized : null;
            avgVelocity = targets.Count > 0 ? sumVelocity / targets.Count : Vector3.zero;
            avgWorldPos = targets.Count > 0 ? sumWorldPos / targets.Count : Vector3.zero;
        }
    }
}

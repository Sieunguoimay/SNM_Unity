using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Planar reflection: a hidden mirror camera rendered to a texture the
    /// surface shader projects back onto the water. One cohesive class
    /// replacing V1's eight (Feature/Installer/Camera/Renderer/Scheduler/
    /// Binder/State/CameraTracker) — the Signal/Effect reactivity is now two
    /// plain change checks.
    ///
    /// Cost model: recompute + render when the source camera (or the water)
    /// actually moved; otherwise re-render only every N frames
    /// (<see cref="WaterReflectionSettings.frameInterval"/>) to pick up scene
    /// changes cheaply.
    /// </summary>
    public class PlanarReflection : IDisposable
    {
        private const float MinMoveSqr = 0.0001f;
        private const float MinAngleDelta = 0.1f;

        private readonly Camera _source;
        private readonly WaterCanvas _canvas;
        private readonly Material _surfaceMaterial;
        private readonly WaterReflectionSettings _settings;

        private readonly Camera _mirror;
        private readonly RenderTexture _texture;
        private readonly Vector3[] _corners = new Vector3[4];

        private Vector3 _lastSourcePos;
        private Quaternion _lastSourceRot;
        private Vector3 _lastCanvasPos;
        private Quaternion _lastCanvasRot;
        private Matrix4x4 _projection;
        private bool _dirty = true;

        /// <summary>Renders performed in the last 60 ticks (for the stats panel).</summary>
        public int RendersLastWindow { get; private set; }
        private int _renderCount;
        private int _tickCount;

        public RenderTexture Texture => _texture;

        public PlanarReflection(
            Camera source,
            WaterCanvas canvas,
            Material surfaceMaterial,
            WaterReflectionSettings settings)
        {
            _source = source;
            _canvas = canvas;
            _surfaceMaterial = surfaceMaterial;
            _settings = settings;

            var go = new GameObject("[WaterReflectionCamera]")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.SetActive(false);

            _mirror = go.AddComponent<Camera>();
            _mirror.clearFlags = CameraClearFlags.SolidColor;
            _mirror.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _mirror.allowHDR = false;
            _mirror.allowMSAA = false;
            _mirror.useOcclusionCulling = false;
            _mirror.depthTextureMode = DepthTextureMode.None;
            SyncLens();

            int width = Mathf.Max(16, settings.textureWidth);
            int height = Mathf.CeilToInt(width / Mathf.Max(0.01f, source.aspect));
            _texture = new RenderTexture(width, height, depth: 16)
            {
                name = "WaterReflection",
            };

            _surfaceMaterial.SetTexture(WaterShaderIds.ReflectionTex, _texture);

            _source.transform.GetPositionAndRotation(out _lastSourcePos, out _lastSourceRot);
            RecomputeProjection();
        }

        /// <summary>Call every LateUpdate. Recomputes when moved, renders when scheduled.</summary>
        public void Tick()
        {
            if (_source == null) return;

            if (SourceMoved() || CanvasMoved())
                RecomputeProjection();

            bool intervalHit = _settings.frameInterval <= 1
                || Time.frameCount % _settings.frameInterval == 0;

            if (_dirty || intervalHit)
            {
                Render();
                _dirty = false;
                _renderCount++;
            }

            if (++_tickCount >= 60)
            {
                RendersLastWindow = _renderCount;
                _renderCount = 0;
                _tickCount = 0;
            }
        }

        private bool SourceMoved()
        {
            _source.transform.GetPositionAndRotation(out var pos, out var rot);
            bool moved = (pos - _lastSourcePos).sqrMagnitude > MinMoveSqr
                || Quaternion.Angle(_lastSourceRot, rot) > MinAngleDelta;
            if (moved)
            {
                _lastSourcePos = pos;
                _lastSourceRot = rot;
            }
            return moved;
        }

        private bool CanvasMoved()
        {
            bool moved = (_canvas.Position - _lastCanvasPos).sqrMagnitude > MinMoveSqr
                || Quaternion.Angle(_lastCanvasRot, _canvas.Rotation) > MinAngleDelta;
            if (moved)
            {
                _lastCanvasPos = _canvas.Position;
                _lastCanvasRot = _canvas.Rotation;
            }
            return moved;
        }

        private void RecomputeProjection()
        {
            var plane = new ReflectionPlane(_canvas);

            // Mirror the source camera across the water plane.
            var srcT = _source.transform;
            _mirror.transform.SetPositionAndRotation(
                plane.ReflectPoint(srcT.position),
                Quaternion.LookRotation(
                    plane.ReflectDirection(srcT.forward),
                    plane.ReflectDirection(srcT.up)));
            SyncLens();

            _canvas.ComputeCorners(_corners);
            _projection = ReflectionProjection.Compute(_mirror, _corners, in plane);

            _surfaceMaterial.SetMatrix(
                WaterShaderIds.ReflectionVP,
                _projection * _mirror.worldToCameraMatrix);

            _dirty = true;
        }

        private void Render()
        {
            var prevProjection = _mirror.projectionMatrix;
            var prevTarget = _mirror.targetTexture;

            _mirror.projectionMatrix = _projection;
            _mirror.targetTexture = _texture;
            _mirror.Render();

            _mirror.projectionMatrix = prevProjection;
            _mirror.targetTexture = prevTarget;
        }

        private void SyncLens()
        {
            _mirror.orthographic = _source.orthographic;
            _mirror.orthographicSize = _source.orthographicSize;
            _mirror.fieldOfView = _source.fieldOfView;
            _mirror.nearClipPlane = _source.nearClipPlane;
            _mirror.farClipPlane = _source.farClipPlane;
            _mirror.aspect = _source.aspect;
        }

        public void Dispose()
        {
            if (_texture != null)
            {
                _texture.Release();
                WaterObjects.Destroy(_texture);
            }
            if (_mirror != null)
                WaterObjects.Destroy(_mirror.gameObject);
        }
    }
}

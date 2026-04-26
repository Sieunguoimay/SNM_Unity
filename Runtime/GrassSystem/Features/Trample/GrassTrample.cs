using System;
using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.GrassSystem
{
    public class GrassTrample : IDisposable
    {
        static readonly int ID_Brushes = Shader.PropertyToID("_Brushes");
        static readonly int ID_BrushCount = Shader.PropertyToID("_BrushCount");
        static readonly int ID_FadeAmount = Shader.PropertyToID("_FadeAmount");
        static readonly int ID_HoldBuffer = Shader.PropertyToID("_HoldBuffer");
        static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");

        SurfaceStampRenderer _renderer;
        StampBuffer _stampBuffer;
        StampBuffer _smoothBuffer;
        SurfaceCanvas _canvas;
        IReadOnlyList<IGrassDisturber> _disturbers = new List<IGrassDisturber>();

        float _fadeSpeed;
        float _holdTime;
        float _minOffset;
        float _proximityTolerance;
        float _grassHeight;

        class TrackState
        {
            public Vector3 PreviousPosition;
            public Vector3 Direction = Vector3.forward;
        }

        readonly SurfaceDisturberTracker<IGrassDisturber, TrackState> _tracker = new();

        public RenderTexture OutputTexture => _renderer.ResultTexture;

        public void Setup(GrassSystemConfig grassConfig, SurfaceCanvas canvas)
        {
            var config = grassConfig.trample;
            _fadeSpeed = config.trampleFadeSpeed;
            _holdTime = Mathf.Max(config.trampleHoldTime, 0.001f);
            _minOffset = config.disturbMinOffset;
            _proximityTolerance = config.proximityTolerance;
            _grassHeight = grassConfig.interactionHeight;
            _canvas = canvas;

#pragma warning disable 618
            var res = grassConfig.placementMap != null
                ? new Vector2Int(grassConfig.placementMap.width, grassConfig.placementMap.height)
                : grassConfig.trampleResolution;
#pragma warning restore 618
            var desc = new RenderTextureDescriptor(res.x, res.y)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };
            var pingPong = new PingPongTexture(desc);
            var material = new Material(config.trampleShader);

            var worldMin = canvas.WorldMin;
            var worldSize = canvas.Size;
            material.SetVector(ID_WorldCanvas, new Vector4(worldMin.x, worldMin.y, worldSize.x, worldSize.y));

            _renderer = new SurfaceStampRenderer(material, pingPong);
            _stampBuffer = new StampBuffer(64);
            _smoothBuffer = new StampBuffer(8);
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _disturbers = disturbers ?? new List<IGrassDisturber>();
        }

        public void ApplyConfig(TrampleConfig config, float interactionHeight)
        {
            _fadeSpeed = config.trampleFadeSpeed;
            _holdTime = Mathf.Max(config.trampleHoldTime, 0.001f);
            _minOffset = config.disturbMinOffset;
            _proximityTolerance = config.proximityTolerance;
            _grassHeight = interactionHeight;
        }

        public void Update(float deltaTime)
        {
            float grassTopY = _canvas.Position.y + _grassHeight;

            _tracker.Sync(_disturbers, (d, state) =>
            {
                state.PreviousPosition = d.WorldPosition;
            });

            foreach (var (d, state) in _tracker.States)
            {
                var pos = d.WorldPosition;

                bool isTouching = d.IsTouchingSurface(grassTopY);
                bool isNear = !isTouching && IsWithinProximity(d, grassTopY);
                if (!isTouching && !isNear) continue;

                float contactRadius = d.GetContactRadius(grassTopY);
                if (contactRadius <= 0f) continue;

                var movement = pos - state.PreviousPosition;
                if (movement.sqrMagnitude > _minOffset * _minOffset)
                {
                    state.Direction = movement.normalized;
                    state.PreviousPosition = pos;
                }

                bool inCanvas = _canvas.Overlaps(pos, contactRadius)
                            || _canvas.Overlaps(state.PreviousPosition, contactRadius);

                if (inCanvas)
                {
                    var dirX = state.Direction.x;
                    var dirZ = state.Direction.z;
                    float angle = Vector2.SqrMagnitude(new Vector2(dirX, dirZ)) > 0.001f
                        ? Mathf.Atan2(dirZ, dirX)
                        : 0f;
                    var stamp = new Vector4(pos.x, pos.z, angle, contactRadius);
                    _stampBuffer.Add(stamp);

                    if (d.SmoothTrample)
                        _smoothBuffer.Add(stamp);
                }
            }

            var mat = _renderer.Material;
            mat.SetFloat(ID_FadeAmount, deltaTime * _fadeSpeed);
            mat.SetFloat(ID_HoldBuffer, _fadeSpeed * _holdTime);
            _stampBuffer.Upload(mat, ID_Brushes, ID_BrushCount);

            _renderer.Render();
        }

        public void UploadSmoothBrushesTo(Material material)
        {
            _smoothBuffer.UploadTo(material, ID_Brushes, ID_BrushCount);
        }

        public void ClearBrushes()
        {
            _stampBuffer.Clear();
            _smoothBuffer.Clear();
        }

        bool IsWithinProximity(IGrassDisturber disturber, float grassTopY)
        {
            if (_proximityTolerance <= 0f) return false;
            float bottomY = disturber.WorldPosition.y;
            return bottomY - grassTopY < _proximityTolerance && bottomY >= grassTopY;
        }

        public DisturberSnapshot[] GetDisturberSnapshots()
        {
            float grassTopY = _canvas.Position.y + _grassHeight;
            var states = _tracker.States;
            var snapshots = new DisturberSnapshot[states.Count];
            int i = 0;
            foreach (var (d, s) in states)
            {
                snapshots[i++] = new DisturberSnapshot
                {
                    Position = d.WorldPosition,
                    Direction = s.Direction,
                    Radius = d.GetContactRadius(grassTopY),
                    InCanvas = _canvas.Contains(d.WorldPosition)
                };
            }
            return snapshots;
        }

        public void Dispose()
        {
            _renderer?.Dispose();
            _renderer = null;
            _tracker.Clear();
        }

        public struct DisturberSnapshot
        {
            public Vector3 Position;
            public Vector3 Direction;
            public float Radius;
            public bool InCanvas;
        }
    }
}

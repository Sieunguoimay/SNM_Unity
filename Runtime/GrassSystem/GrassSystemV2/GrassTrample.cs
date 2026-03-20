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
        static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");

        SurfaceStampRenderer _renderer;
        StampBuffer _stampBuffer;
        SurfaceCanvas _canvas;
        float _fadeSpeed;
        float _minOffset;

        struct TrackState
        {
            public Vector3 PreviousPosition;
            public Vector3 Direction;
        }

        readonly Dictionary<IGrassDisturber, TrackState> _states = new();
        readonly HashSet<IGrassDisturber> _seen = new();

        public RenderTexture OutputTexture => _renderer.ResultTexture;

        public void Setup(GrassSystemV2Config config, SurfaceCanvas canvas)
        {
            _fadeSpeed = config.trampleFadeSpeed;
            _minOffset = config.disturbMinOffset;
            _canvas = canvas;

            int res = config.trampleResolution;
            var desc = new RenderTextureDescriptor(res, res)
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
        }

        public void Update(IReadOnlyList<IGrassDisturber> disturbers, float deltaTime)
        {
            SyncAndTrack(disturbers);

            var mat = _renderer.Material;
            mat.SetFloat(ID_FadeAmount, deltaTime * _fadeSpeed);
            _stampBuffer.Upload(mat, ID_Brushes, ID_BrushCount);

            _renderer.Render();
        }

        void SyncAndTrack(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _seen.Clear();

            for (int i = 0; i < disturbers.Count; i++)
            {
                var d = disturbers[i];
                _seen.Add(d);

                var pos = d.WorldPosition;

                if (!_states.TryGetValue(d, out var state))
                {
                    state = new TrackState { PreviousPosition = pos, Direction = Vector3.zero };
                    _states[d] = state;
                    continue;
                }

                var movement = pos - state.PreviousPosition;
                if (movement.sqrMagnitude > _minOffset * _minOffset)
                {
                    state.Direction = movement.normalized;
                    state.PreviousPosition = pos;
                    _states[d] = state;
                }

                bool inCanvas = _canvas.Contains(pos) || _canvas.Contains(state.PreviousPosition);

                if (inCanvas)
                {
                    // Use movement direction if available, otherwise signal radial presence
                    const float presenceSentinel = 1000f;
                    float angle = state.Direction.sqrMagnitude > 0.001f
                        ? Mathf.Atan2(state.Direction.z, state.Direction.x)
                        : presenceSentinel;
                    _stampBuffer.Add(new Vector4(pos.x, pos.z, angle, d.GrassContactRadius));
                }
            }

            // Remove stale disturbers
            var toRemove = new List<IGrassDisturber>();
            foreach (var kvp in _states)
            {
                if (!_seen.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            for (int i = 0; i < toRemove.Count; i++)
                _states.Remove(toRemove[i]);
        }

        public DisturberSnapshot[] GetDisturberSnapshots()
        {
            var snapshots = new DisturberSnapshot[_states.Count];
            int i = 0;
            foreach (var kvp in _states)
            {
                var d = kvp.Key;
                var s = kvp.Value;
                snapshots[i++] = new DisturberSnapshot
                {
                    Position = d.WorldPosition,
                    Direction = s.Direction,
                    Radius = d.GrassContactRadius,
                    InCanvas = _canvas.Contains(d.WorldPosition)
                };
            }
            return snapshots;
        }

        public void Dispose()
        {
            _renderer?.Dispose();
            _renderer = null;
            _states.Clear();
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

using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public readonly struct DisturberInfo
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly float ContactRadius;
        public readonly bool IsInWater;

        public DisturberInfo(IWaveDisturber disturber, bool isInWater, float contactRadius)
        {
            Position = disturber.WorldPosition;
            Velocity = disturber.WorldVelocity;
            ContactRadius = contactRadius;
            IsInWater = isInWater;
        }
    }

    public class WaveDisturberTracker
    {
        private readonly IEnumerable<IWaveDisturber> _source;
        private readonly SurfaceCanvas _canvas;
        private readonly IWaveSimulation _waveSimulation;
        private readonly WaveDisturberConfig _config;

        private readonly Dictionary<IWaveDisturber, DisturberState> _states = new();
        private readonly List<DisturberInfo> _infoSnapshot = new();
        private readonly HashSet<IWaveDisturber> _activeSet = new();
        private readonly List<IWaveDisturber> _toRemove = new();

        public IReadOnlyList<DisturberInfo> Snapshot => _infoSnapshot;
        public WaveDisturberConfig Config => _config;

        private class DisturberState
        {
            public bool wasInWater;
            public Vector3 lastPosition;
            public Vector2 lastWakeUV;
            public bool hasLastWakeUV;
        }

        public WaveDisturberTracker(
            IEnumerable<IWaveDisturber> source,
            SurfaceCanvas canvas,
            IWaveSimulation waveSimulation,
            WaveDisturberConfig config)
        {
            _source = source;
            _canvas = canvas;
            _waveSimulation = waveSimulation;
            _config = config;
        }

        public void Update(float deltaTime)
        {
            if (!_config.enabled) return;

            SyncStates();

            float waterY = _canvas.Position.y;

            _infoSnapshot.Clear();

            foreach (var (disturber, state) in _states)
            {
                bool isInWater = disturber.IsTouchingWater(waterY);
                float contactRadius = disturber.GetContactRadius(waterY);

                // Relaxed proximity check: allow wake when slightly above water surface
                bool isNearWater = isInWater || IsWithinProximity(disturber, waterY);

                if (isInWater && !state.wasInWater)
                {
                    // Entry splash
                    float speed = disturber.WorldVelocity.magnitude;
                    float strength = Mathf.Clamp(speed * _config.entryStrengthScale, 0f, _config.entryMaxStrength);
                    AddDisturbance(disturber.WorldPosition, contactRadius, strength);
                    // Seed the wake trail from the entry point
                    state.lastWakeUV = _canvas.WorldToUV(disturber.WorldPosition);
                    state.hasLastWakeUV = true;
                    state.lastPosition = disturber.WorldPosition;
                }
                else if (isNearWater && contactRadius > 0f)
                {
                    // Continuous wake — emit a disturbance when the object has moved
                    // far enough. Use full 3D distance (not just UV) so vertical
                    // bobbing through the surface also generates ripples.
                    // Gated on contactRadius > 0: fully submerged objects (no surface
                    // intersection) produce no surface disturbance.
                    Vector2 currentUV = _canvas.WorldToUV(disturber.WorldPosition);

                    if (!state.hasLastWakeUV)
                    {
                        state.lastWakeUV = currentUV;
                        state.hasLastWakeUV = true;
                    }

                    float worldDist = Vector3.Distance(disturber.WorldPosition, state.lastPosition);
                    float speed = disturber.WorldVelocity.magnitude;
                    float speedT = Mathf.Clamp01((speed - _config.wakeMinSpeed) / (_config.wakeMaxSpeed - _config.wakeMinSpeed));
                    float curveT = _config.wakeSpeedCurve.Evaluate(speedT);
                    float stepThreshold = Mathf.Lerp(_config.wakeSpacingAtMinSpeed, _config.wakeSpacingAtFullSpeed, curveT);

                    if (worldDist > stepThreshold)
                    {
                        float strength = Mathf.Lerp(0f, _config.wakeStrength, curveT);
                        AddDisturbanceUV(currentUV, contactRadius, strength);
                        state.lastWakeUV = currentUV;
                        state.lastPosition = disturber.WorldPosition;
                    }
                }

                if (!isNearWater)
                {
                    state.hasLastWakeUV = false;
                }

                state.wasInWater = isInWater;

                _infoSnapshot.Add(new DisturberInfo(disturber, isInWater, contactRadius));
            }
        }

        private bool IsWithinProximity(IWaveDisturber disturber, float waterY)
        {
            if (_config.proximityTolerance <= 0f) return false;
            float bottomY = disturber.WorldPosition.y;
            return bottomY - waterY < _config.proximityTolerance && bottomY >= waterY;
        }

        private void SyncStates()
        {
            _activeSet.Clear();

            foreach (var disturber in _source)
            {
                _activeSet.Add(disturber);
                if (!_states.ContainsKey(disturber))
                    _states[disturber] = new DisturberState { lastPosition = disturber.WorldPosition };
            }

            _toRemove.Clear();
            foreach (var key in _states.Keys)
            {
                if (!_activeSet.Contains(key))
                    _toRemove.Add(key);
            }
            foreach (var key in _toRemove) _states.Remove(key);
        }

        private void AddDisturbance(Vector3 worldPos, float worldRadius, float strength)
        {
            Vector2 uvPos = _canvas.WorldToUV(worldPos);
            AddDisturbanceUV(uvPos, worldRadius, strength);
        }

        private void AddDisturbanceUV(Vector2 uvPos, float worldRadius, float strength)
        {
            if (uvPos.x < 0f || uvPos.x > 1f || uvPos.y < 0f || uvPos.y > 1f) return;

            float uvRadius = worldRadius > 0f
                ? worldRadius / Mathf.Max(_canvas.Size.x, _canvas.Size.y)
                : 0f;

            // Enforce minimum radius so wake is always visible (convert world → UV)
            float minUVRadius = _config.wakeMinRadiusWorld / Mathf.Max(_canvas.Size.x, _canvas.Size.y);
            uvRadius = Mathf.Max(uvRadius, minUVRadius);

            _waveSimulation.AddDisturbance(new WaveDisturbance
            {
                uvPos = uvPos,
                radius = uvRadius,
                strength = strength
            });
        }
    }
}

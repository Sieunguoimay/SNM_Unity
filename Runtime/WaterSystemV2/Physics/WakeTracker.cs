using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Turns registered <see cref="IWaterDisturber"/>s into wave disturbances:
    /// an entry splash when an object breaks the surface, then a wake trail
    /// spaced by distance while it moves through (or hovers just above) the
    /// water. Consumes the global <see cref="WaterInteraction"/> registry —
    /// per-object state is created on first sight and dropped on unregister,
    /// so nothing is diffed or allocated per frame (V1 re-synced a
    /// dictionary/hash-set every frame).
    /// </summary>
    public class WakeTracker : IDisposable
    {
        private readonly WaterCanvas _canvas;
        private readonly WaveSimulation _sim;
        private readonly WaterWaveSettings.DisturberSettings _settings;
        private readonly Dictionary<IWaterDisturber, State> _states = new();

        /// <summary>Disturbers touching or hovering over this water right now (for the stats panel).</summary>
        public int ActiveCount { get; private set; }

        private class State
        {
            public bool wasInWater;
            public bool hasWakeTrail;
            public Vector3 lastPosition;
        }

        public WakeTracker(WaterCanvas canvas, WaveSimulation sim, WaterWaveSettings.DisturberSettings settings)
        {
            _canvas = canvas;
            _sim = sim;
            _settings = settings;
            WaterInteraction.DisturberRemoved += OnDisturberRemoved;
        }

        public void Tick()
        {
            if (!_settings.enabled) return;

            float waterY = _canvas.SurfaceY;
            int active = 0;

            var disturbers = WaterInteraction.Disturbers;
            for (int i = 0; i < disturbers.Count; i++)
            {
                var disturber = disturbers[i];
                var position = disturber.WorldPosition;

                // Objects far outside this water: forget their trail and move on.
                if (!_canvas.Overlaps(position, 1f))
                {
                    if (_states.TryGetValue(disturber, out var stale))
                    {
                        stale.wasInWater = false;
                        stale.hasWakeTrail = false;
                    }
                    continue;
                }

                var state = GetOrAddState(disturber);
                bool isInWater = disturber.IsTouchingSurface(waterY);
                float contactRadius = disturber.GetContactRadius(waterY);
                bool isNearWater = isInWater || IsWithinProximity(position, waterY);

                if (isInWater || isNearWater) active++;

                if (isInWater && !state.wasInWater)
                {
                    // Entry splash, scaled by impact speed.
                    float speed = disturber.WorldVelocity.magnitude;
                    float strength = Mathf.Clamp(speed * _settings.entryStrengthScale, 0f, _settings.entryMaxStrength);
                    Emit(position, contactRadius, strength);

                    state.hasWakeTrail = true;
                    state.lastPosition = position;
                }
                else if (isNearWater && contactRadius > 0f)
                {
                    // Continuous wake — emit when the object moved far enough.
                    // Full 3D distance, so vertical bobbing also ripples.
                    // Gated on contactRadius > 0: fully submerged objects leave
                    // no surface trail.
                    if (!state.hasWakeTrail)
                    {
                        state.hasWakeTrail = true;
                        state.lastPosition = position;
                    }

                    float speed = disturber.WorldVelocity.magnitude;
                    float speedRange = _settings.wakeMaxSpeed - _settings.wakeMinSpeed;
                    float speedT = speedRange > 0f
                        ? Mathf.Clamp01((speed - _settings.wakeMinSpeed) / speedRange)
                        : (speed >= _settings.wakeMinSpeed ? 1f : 0f);
                    float curveT = _settings.wakeSpeedCurve.Evaluate(speedT);
                    float spacing = Mathf.Lerp(_settings.wakeSpacingAtMinSpeed, _settings.wakeSpacingAtFullSpeed, curveT);

                    if (Vector3.Distance(position, state.lastPosition) > spacing)
                    {
                        Emit(position, contactRadius, Mathf.Lerp(0f, _settings.wakeStrength, curveT));
                        state.lastPosition = position;
                    }
                }

                if (!isNearWater)
                    state.hasWakeTrail = false;

                state.wasInWater = isInWater;
            }

            ActiveCount = active;
        }

        /// <summary>Manual disturbance entry point (explosions, scripted splashes).</summary>
        public void Emit(Vector3 worldPos, float worldRadius, float strength)
        {
            Vector2 uv = _canvas.WorldToUV(worldPos);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return;

            float uvRadius = Mathf.Max(
                _canvas.WorldRadiusToUV(Mathf.Max(worldRadius, 0f)),
                _canvas.WorldRadiusToUV(_settings.wakeMinRadiusWorld));

            _sim.AddDisturbance(uv, uvRadius, strength);
        }

        private bool IsWithinProximity(Vector3 position, float waterY)
        {
            if (_settings.proximityTolerance <= 0f) return false;
            float bottomY = position.y;
            return bottomY >= waterY && bottomY - waterY < _settings.proximityTolerance;
        }

        private State GetOrAddState(IWaterDisturber disturber)
        {
            if (!_states.TryGetValue(disturber, out var state))
            {
                state = new State { lastPosition = disturber.WorldPosition };
                _states[disturber] = state;
            }
            return state;
        }

        private void OnDisturberRemoved(IWaterDisturber disturber) => _states.Remove(disturber);

        public void Dispose()
        {
            WaterInteraction.DisturberRemoved -= OnDisturberRemoved;
            _states.Clear();
        }
    }
}

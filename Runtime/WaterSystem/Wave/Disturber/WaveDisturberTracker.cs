using System.Collections.Generic;
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    /// <summary>
    /// Polls a live list of <see cref="IWaveDisturber"/> objects every frame,
    /// detects water entry and movement, and feeds <see cref="WaveDisturbance"/>
    /// events into the GPU wave simulation.
    /// </summary>
    public class WaveDisturberTracker
    {
        private readonly IEnumerable<IWaveDisturber> _source;
        private readonly SurfaceData                 _surface;
        private readonly IWaveSimulation             _waveSimulation;
        private readonly WaveDisturberConfig         _config;

        private readonly Dictionary<IWaveDisturber, DisturberState> _states = new();

        private class DisturberState
        {
            public bool    wasInWater;
            public Vector3 lastPosition;
            public float   wakeTimer;
        }

        public WaveDisturberTracker(
            IEnumerable<IWaveDisturber> source,
            SurfaceData                surface,
            IWaveSimulation            waveSimulation,
            WaveDisturberConfig        config)
        {
            _source         = source;
            _surface        = surface;
            _waveSimulation = waveSimulation;
            _config         = config;
        }

        public void Update(float deltaTime)
        {
            // Sync state dictionary to current source snapshot
            SyncStates();

            float waterY = _surface.position.y;

            foreach (var (disturber, state) in _states)
            {
                bool isInWater = disturber.WorldPosition.y < waterY;

                if (isInWater && !state.wasInWater)
                {
                    // Entry: strength driven by impact velocity
                    float speed    = disturber.WorldVelocity.magnitude;
                    float strength = Mathf.Clamp(speed * _config.entryStrengthScale, 0f, _config.maxEntryStrength);
                    AddDisturbance(disturber.WorldPosition, disturber.Radius, strength);
                }
                else if (isInWater)
                {
                    // Wake: periodic small pulses while moving through water
                    state.wakeTimer += deltaTime;
                    if (state.wakeTimer >= _config.wakeInterval)
                    {
                        state.wakeTimer = 0f;
                        float speed = disturber.WorldVelocity.magnitude;
                        if (speed > _config.wakeMinSpeed)
                            AddDisturbance(disturber.WorldPosition, disturber.Radius * 0.5f, _config.wakeStrength);
                    }
                }

                state.wasInWater  = isInWater;
                state.lastPosition = disturber.WorldPosition;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void SyncStates()
        {
            // Add newly registered disturbers
            foreach (var disturber in _source)
            {
                if (!_states.ContainsKey(disturber))
                    _states[disturber] = new DisturberState { lastPosition = disturber.WorldPosition };
            }

            // Remove disturbers no longer in the source (build removal list to avoid modify-during-iterate)
            var toRemove = new List<IWaveDisturber>();
            foreach (var key in _states.Keys)
            {
                bool found = false;
                foreach (var d in _source) { if (d == key) { found = true; break; } }
                if (!found) toRemove.Add(key);
            }
            foreach (var key in toRemove) _states.Remove(key);
        }

        private void AddDisturbance(Vector3 worldPos, float worldRadius, float strength)
        {
            Vector2 uvPos = WorldToUV(worldPos);
            if (uvPos.x < 0f || uvPos.x > 1f || uvPos.y < 0f || uvPos.y > 1f) return;

            float uvRadius = worldRadius / Mathf.Max(_surface.size.x, _surface.size.y);

            _waveSimulation.AddDisturbance(new WaveDisturbance
            {
                uvPos    = uvPos,
                radius   = uvRadius,
                strength = strength
            });
        }

        private Vector2 WorldToUV(Vector3 worldPos)
        {
            // Transform world position into the water surface's local XZ plane
            Vector3 local = Quaternion.Inverse(_surface.rotation) * (worldPos - _surface.position);
            float u = local.x / _surface.size.x + 0.5f;
            float v = local.z / _surface.size.y + 0.5f;
            return new Vector2(u, v);
        }
    }
}

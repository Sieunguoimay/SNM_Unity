using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Archimedes buoyancy for registered <see cref="IWaterFloater"/>s:
    /// upward force from submerged volume, drag blended toward in-water
    /// values, and a dampener near the surface to calm bobbing. Runs on
    /// FixedUpdate. Consumes the global <see cref="WaterInteraction"/>
    /// registry; original rigidbody drag is captured once and restored when
    /// the object exits the water or unregisters.
    ///
    /// Note: overlapping water bodies are unsupported — with two waters, the
    /// one containing the object applies forces, but both may fight over
    /// drag restoration.
    /// </summary>
    public class BuoyancySolver : IDisposable
    {
        private readonly WaterCanvas _canvas;
        private readonly WaterBuoyancySettings _settings;
        private readonly Dictionary<IWaterFloater, State> _states = new();

        /// <summary>Floaters at least partially submerged right now (for the stats panel).</summary>
        public int SubmergedCount { get; private set; }

        private class State
        {
            public float originalLinearDrag;
            public float originalAngularDrag;
            public bool wasSubmerged;
            public bool dragCaptured;
        }

        public BuoyancySolver(WaterCanvas canvas, WaterBuoyancySettings settings)
        {
            _canvas = canvas;
            _settings = settings;
            WaterInteraction.FloaterRemoved += OnFloaterRemoved;
        }

        public void Tick()
        {
            if (!_settings.enabled) return;

            float waterY = _canvas.SurfaceY;
            float gravityMag = Mathf.Abs(UnityEngine.Physics.gravity.y);
            int submerged = 0;

            var floaters = WaterInteraction.Floaters;
            for (int i = 0; i < floaters.Count; i++)
            {
                var floater = floaters[i];
                var body = floater.Rigidbody;
                if (body == null || body.isKinematic) continue;

                var state = GetOrAddState(floater);

                if (!_canvas.Contains(floater.WorldPosition))
                {
                    RestoreDrag(body, state);
                    continue;
                }

                if (!state.dragCaptured)
                {
                    state.originalLinearDrag = body.linearDamping;
                    state.originalAngularDrag = body.angularDamping;
                    state.dragCaptured = true;
                }

                float totalVolume = floater.GetTotalVolume();
                if (totalVolume <= 0f) continue;

                float submergedVolume = floater.GetSubmergedVolume(waterY);
                float ratio = Mathf.Clamp01(submergedVolume / totalVolume);

                if (ratio > 0f)
                {
                    submerged++;

                    float buoyancyForce = _settings.waterDensity * submergedVolume * gravityMag;
                    body.AddForce(buoyancyForce * Vector3.up, ForceMode.Force);

                    body.linearDamping = Mathf.Lerp(state.originalLinearDrag, _settings.linearDragInWater, ratio);
                    body.angularDamping = Mathf.Lerp(state.originalAngularDrag, _settings.angularDragInWater, ratio);

                    // Calm vertical oscillation while straddling the surface.
                    if (ratio > 0.2f && ratio < 0.8f)
                    {
                        float dampening = -body.linearVelocity.y * _settings.surfaceDampening * body.mass;
                        body.AddForce(dampening * Vector3.up, ForceMode.Force);
                    }

                    state.wasSubmerged = true;
                }
                else
                {
                    RestoreDrag(body, state);
                }
            }

            SubmergedCount = submerged;
        }

        private static void RestoreDrag(Rigidbody body, State state)
        {
            if (!state.wasSubmerged) return;
            body.linearDamping = state.originalLinearDrag;
            body.angularDamping = state.originalAngularDrag;
            state.wasSubmerged = false;
        }

        private State GetOrAddState(IWaterFloater floater)
        {
            if (!_states.TryGetValue(floater, out var state))
            {
                state = new State();
                _states[floater] = state;
            }
            return state;
        }

        private void OnFloaterRemoved(IWaterFloater floater)
        {
            if (_states.TryGetValue(floater, out var state)
                && state.wasSubmerged
                && floater.Rigidbody != null)
            {
                RestoreDrag(floater.Rigidbody, state);
            }
            _states.Remove(floater);
        }

        public void Dispose()
        {
            WaterInteraction.FloaterRemoved -= OnFloaterRemoved;
            _states.Clear();
        }
    }
}

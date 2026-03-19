using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Buoyancy
{
    public class BuoyancyTracker
    {
        private readonly IEnumerable<IBuoyant> _source;
        private readonly SurfaceCanvas _canvas;
        private readonly BuoyancyConfig _config;

        private readonly Dictionary<IBuoyant, BuoyantState> _states = new();
        private readonly HashSet<IBuoyant> _activeSet = new();
        private readonly List<IBuoyant> _toRemove = new();

        private class BuoyantState
        {
            public float originalLinearDrag;
            public float originalAngularDrag;
            public bool wasSubmerged;
            public bool dragCaptured;
        }

        public BuoyancyTracker(
            IEnumerable<IBuoyant> source,
            SurfaceCanvas canvas,
            BuoyancyConfig config)
        {
            _source = source;
            _canvas = canvas;
            _config = config;
        }

        public void Update(float fixedDeltaTime)
        {
            if (!_config.enabled) return;

            SyncStates();

            float waterY = _canvas.Position.y;
            float gravityMag = Mathf.Abs(Physics.gravity.y);

            foreach (var (buoyant, state) in _states)
            {
                var body = buoyant.Rigidbody;
                if (body == null || body.isKinematic) continue;

                // Skip objects outside the water surface area
                if (!_canvas.Contains(buoyant.WorldPosition))
                {
                    if (state.wasSubmerged)
                    {
                        body.linearDamping = state.originalLinearDrag;
                        body.angularDamping = state.originalAngularDrag;
                        state.wasSubmerged = false;
                    }
                    continue;
                }

                // Capture original drag on first non-kinematic encounter
                if (!state.dragCaptured)
                {
                    state.originalLinearDrag = body.linearDamping;
                    state.originalAngularDrag = body.angularDamping;
                    state.dragCaptured = true;
                }

                float totalVolume = buoyant.GetTotalVolume();
                if (totalVolume <= 0f) continue;

                float submergedVolume = buoyant.GetSubmergedVolume(waterY);
                float ratio = Mathf.Clamp01(submergedVolume / totalVolume);

                if (ratio > 0f)
                {
                    // Buoyancy force: Archimedes' principle
                    float buoyancyForce = _config.waterDensity * submergedVolume * gravityMag;
                    body.AddForce(buoyancyForce * Vector3.up, ForceMode.Force);

                    // Water drag: lerp from original toward in-water values based on submersion
                    body.linearDamping = Mathf.Lerp(state.originalLinearDrag, _config.linearDragInWater, ratio);
                    body.angularDamping = Mathf.Lerp(state.originalAngularDrag, _config.angularDragInWater, ratio);

                    // Surface dampening: reduce bobbing when near the surface
                    if (ratio > 0.2f && ratio < 0.8f)
                    {
                        float verticalVelocity = body.linearVelocity.y;
                        float dampeningForce = -verticalVelocity * _config.surfaceDampening * body.mass;
                        body.AddForce(dampeningForce * Vector3.up, ForceMode.Force);
                    }

                    state.wasSubmerged = true;
                }
                else if (state.wasSubmerged)
                {
                    // Exited water — restore original drag
                    body.linearDamping = state.originalLinearDrag;
                    body.angularDamping = state.originalAngularDrag;
                    state.wasSubmerged = false;
                }
            }
        }

        private void SyncStates()
        {
            _activeSet.Clear();

            foreach (var buoyant in _source)
            {
                _activeSet.Add(buoyant);
                if (!_states.ContainsKey(buoyant))
                    _states[buoyant] = new BuoyantState();
            }

            _toRemove.Clear();
            foreach (var key in _states.Keys)
            {
                if (!_activeSet.Contains(key))
                    _toRemove.Add(key);
            }
            foreach (var key in _toRemove) _states.Remove(key);
        }
    }
}

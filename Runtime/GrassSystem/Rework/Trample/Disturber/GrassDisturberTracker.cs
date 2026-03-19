using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDisturberTracker
    {
        private readonly List<IGrassDisturber> _localDisturbers = new();
        private IReadOnlyList<IGrassDisturber> _externalDisturbers;
        private readonly float _minOffset;
        private readonly SurfaceCanvas _canvas;

        private readonly Dictionary<IGrassDisturber, DisturberState> _states = new();
        private readonly HashSet<IGrassDisturber> _activeSet = new();
        private readonly List<IGrassDisturber> _toRemove = new();

        private class DisturberState
        {
            public Vector3 previousPosition;
            public Vector3 direction;
        }

        public GrassDisturberTracker(float minOffset, SurfaceCanvas canvas)
        {
            _minOffset = minOffset;
            _canvas = canvas;
        }

        public void SetExternalDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _externalDisturbers = disturbers;
        }

        public void RegisterLocal(IGrassDisturber disturber)
        {
            _localDisturbers.Add(disturber);
        }

        public void Update(StampBuffer stampBuffer)
        {
            SyncStates();

            foreach (var (disturber, state) in _states)
            {
                var currPos = disturber.WorldPosition;

                // Compute direction from position delta
                var movement = currPos - state.previousPosition;
                if (movement.sqrMagnitude > _minOffset * _minOffset)
                {
                    state.direction = movement.normalized;
                    state.previousPosition = currPos;
                }

                // Check canvas bounds
                bool isValid = _canvas.Contains(currPos) || _canvas.Contains(state.previousPosition);
                if (!isValid) continue;

                float angle = Mathf.Atan2(state.direction.z, state.direction.x);
                stampBuffer.Add(new Vector4(currPos.x, currPos.z, angle, disturber.GrassContactRadius));
            }
        }

        private void SyncStates()
        {
            _activeSet.Clear();

            for (int i = 0; i < _localDisturbers.Count; i++)
            {
                var d = _localDisturbers[i];
                _activeSet.Add(d);
                if (!_states.ContainsKey(d))
                    _states[d] = new DisturberState { previousPosition = d.WorldPosition };
            }

            if (_externalDisturbers != null)
            {
                for (int i = 0; i < _externalDisturbers.Count; i++)
                {
                    var d = _externalDisturbers[i];
                    _activeSet.Add(d);
                    if (!_states.ContainsKey(d))
                        _states[d] = new DisturberState { previousPosition = d.WorldPosition };
                }
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

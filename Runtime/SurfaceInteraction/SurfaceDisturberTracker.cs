using System;
using System.Collections.Generic;

namespace Snm.SurfaceInteraction
{
    public class SurfaceDisturberTracker<TDisturber, TState>
        where TDisturber : class
        where TState : class, new()
    {
        readonly Dictionary<TDisturber, TState> _states = new();
        readonly HashSet<TDisturber> _activeSet = new();
        readonly List<TDisturber> _removeBuffer = new();

        public Dictionary<TDisturber, TState> States => _states;

        public void Sync(IEnumerable<TDisturber> source, Action<TDisturber, TState> onAdd = null)
        {
            _activeSet.Clear();

            foreach (var disturber in source)
            {
                _activeSet.Add(disturber);
                if (!_states.ContainsKey(disturber))
                {
                    var state = new TState();
                    onAdd?.Invoke(disturber, state);
                    _states[disturber] = state;
                }
            }

            _removeBuffer.Clear();
            foreach (var key in _states.Keys)
            {
                if (!_activeSet.Contains(key))
                    _removeBuffer.Add(key);
            }
            foreach (var key in _removeBuffer)
                _states.Remove(key);
        }

        public void Clear() => _states.Clear();
    }
}

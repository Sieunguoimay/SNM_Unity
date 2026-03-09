using System;
using System.Collections.Generic;

namespace Snm.Reactivity
{
    public class Reaction : IDisposable
    {
        private readonly Action _callback;
        private readonly HashSet<ISignal> _trackedSignals = new();
        private bool _isExecuting;

        public Reaction(Action callback)
        {
            _callback = callback;
            Execute();
        }

        public void Dispose()
        {
            UntrackAllSignals();
        }

        public void Execute()
        {
            if (_isExecuting) return;
            _isExecuting = true;

            UntrackAllSignals();

            using var _ = new ReactionContext(this);

            _callback();

            _isExecuting = false;
        }

        internal void TrackSignal(ISignal signal)
        {
            _trackedSignals.Add(signal);
        }

        private void UntrackAllSignals()
        {
            foreach (var signal in _trackedSignals)
            {
                signal.Unsubscribe(this);
            }
            _trackedSignals.Clear();
        }
    }
}

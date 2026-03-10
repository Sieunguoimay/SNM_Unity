using System;
using System.Collections.Generic;

namespace Snm.Reactivity
{
    public class Effect : IDisposable
    {
        private readonly Action _callback;
        private readonly HashSet<ISignal> _trackedSignals = new();
        private bool _isExecuting;

        public Effect(Action callback)
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

            try
            {
                using var _ = new EffectContext(this);
                _callback();
            }
            finally
            {
                _isExecuting = false;
            }
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

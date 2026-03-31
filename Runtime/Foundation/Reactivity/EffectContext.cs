using System;
using System.Collections.Generic;

namespace Snm.Reactivity
{
    public class EffectContext : IDisposable
    {
        private static readonly Stack<Effect> _trackingStack = new();

        private readonly Effect _effect;
        private bool _isDisposed;

        public static Effect ActiveEffect => _trackingStack.Count > 0 ? _trackingStack.Peek() : null;

        public EffectContext(Effect effect)
        {
            _effect = effect;
            _trackingStack.Push(effect);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_trackingStack.Count > 0 && _trackingStack.Peek() == _effect)
                _trackingStack.Pop();
        }
    }
}

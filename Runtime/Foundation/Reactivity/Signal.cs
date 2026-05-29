using System;
using System.Collections.Generic;

namespace Snm.Reactivity
{
    internal interface ISignal
    {
        void Unsubscribe(Effect effect);
    }

    public class Signal<T> : ISignal
    {
        private readonly HashSet<Effect> _subscribers = new();
        private readonly List<Effect> _notifyBuffer = new();
        private readonly HashSet<Action<T>> _listeners = new();
        private readonly List<Action<T>> _listenerBuffer = new();

        private T _value;
        private bool _isNotifying;
        private bool _hasPendingNotify;

        public T Value
        {
            get
            {
                TrackCurrentEffect();

                return _value;
            }

            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    NotifySubscribers();
                }
            }
        }

        public Signal(T initialValue)
        {
            _value = initialValue;
        }

        void ISignal.Unsubscribe(Effect effect)
        {
            _subscribers.Remove(effect);
        }

        public IDisposable Subscribe(Action<T> listener)
        {
            _listeners.Add(listener);
            listener(_value);
            return new Unsubscriber(() => _listeners.Remove(listener));
        }

        private void NotifySubscribers()
        {
            // Re-entrant writes (a subscriber/listener sets this signal during its own notification)
            // would otherwise clear the shared _notifyBuffer mid-iteration and corrupt the outer loop.
            // Queue the re-entrant write and let the outer loop pick it up on the next pass.
            if (_isNotifying)
            {
                _hasPendingNotify = true;
                return;
            }

            _isNotifying = true;
            try
            {
                do
                {
                    _hasPendingNotify = false;

                    _notifyBuffer.Clear();
                    _notifyBuffer.AddRange(_subscribers);
                    foreach (var subscriber in _notifyBuffer)
                    {
                        subscriber.Execute();
                    }

                    _listenerBuffer.Clear();
                    _listenerBuffer.AddRange(_listeners);
                    foreach (var listener in _listenerBuffer)
                    {
                        listener(_value);
                    }
                } while (_hasPendingNotify);
            }
            finally
            {
                _isNotifying = false;
            }
        }

        private void TrackCurrentEffect()
        {
            var effect = EffectContext.ActiveEffect;
            if (effect != null)
            {
                _subscribers.Add(effect);
                effect.TrackSignal(this);
            }
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly Action _onDispose;
            public Unsubscriber(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }
    }
}

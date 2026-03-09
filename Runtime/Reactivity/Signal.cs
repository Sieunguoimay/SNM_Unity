using System.Collections.Generic;

namespace Snm.Reactivity
{
    internal interface ISignal
    {
        void Unsubscribe(Reaction reaction);
    }

    public class Signal<T> : ISignal
    {
        private readonly HashSet<Reaction> _subscribers = new();
        private readonly List<Reaction> _notifyBuffer = new();

        private T _value;

        public T Value
        {
            get
            {
                TrackCurrentReaction();

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

        void ISignal.Unsubscribe(Reaction reaction)
        {
            _subscribers.Remove(reaction);
        }

        private void NotifySubscribers()
        {
            _notifyBuffer.Clear();
            _notifyBuffer.AddRange(_subscribers);
            foreach (var subscriber in _notifyBuffer)
            {
                subscriber.Execute();
            }
        }

        private void TrackCurrentReaction()
        {
            var reaction = ReactionContext.ActiveReaction;
            if (reaction != null)
            {
                _subscribers.Add(reaction);
                reaction.TrackSignal(this);
            }
        }
    }
}

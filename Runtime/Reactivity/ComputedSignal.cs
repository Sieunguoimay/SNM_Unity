using System;

namespace Snm.Reactivity
{
    public class ComputedSignal<T> : Signal<T>, IDisposable
    {
        private readonly Reaction _reaction;

        public ComputedSignal(Func<T> compute) : base(default)
        {
            _reaction = new Reaction(() => Value = compute());
        }

        public void Dispose() => _reaction.Dispose();
    }
}

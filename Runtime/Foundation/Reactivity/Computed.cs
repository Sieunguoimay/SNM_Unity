using System;

namespace Snm.Reactivity
{
    public class Computed<T> : Signal<T>, IDisposable
    {
        private readonly Effect _effect;

        public Computed(Func<T> compute) : base(default)
        {
            _effect = new Effect(() => Value = compute());
        }

        public void Dispose() => _effect.Dispose();
    }
}

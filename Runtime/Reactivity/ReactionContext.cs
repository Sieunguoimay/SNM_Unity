using System;
using System.Collections.Generic;

namespace Snm.Reactivity
{
    public class ReactionContext : IDisposable
    {
        private static readonly Stack<Reaction> _trackingStack = new();

        private readonly Reaction _reaction;
        private bool _isDisposed;

        public static Reaction ActiveReaction => _trackingStack.Count > 0 ? _trackingStack.Peek() : null;

        public ReactionContext(Reaction reaction)
        {
            _reaction = reaction;
            _trackingStack.Push(reaction);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_trackingStack.Count > 0 && _trackingStack.Peek() == _reaction)
                _trackingStack.Pop();
        }
    }
}

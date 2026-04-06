using System;

namespace Snm.DependencyInjection
{
    public interface IScopeFactory
    {
        IManagedScope CreateScope(Action<IBindingContext> configure);
    }
}
/*
  RuntimeContainer already satisfies IManagedScope (IResolver + IDisposable) — it just doesn't declare it. And its CreateScope
  already returns RuntimeContainer, which would be IManagedScope.

  DI layer:
    IManagedScope : IResolver, IDisposable, IScopeFactory
    RuntimeContainer : IManagedScope   ← just add the declaration

  Hosting layer:
    LifecycleScope : IManagedScope     ← renamed from ManagedScope, adds lifecycle
    AppScopeFactory : IScopeFactory    ← creates LifecycleScopes

  One thing to add: IManagedScope should extend IScopeFactory too, so any scope can create children. That way WorldObjectSpawner
  takes IScopeFactory (or IManagedScope) and creates raw child scopes — no casts anywhere.

  The only cast left would be inside AppScopeFactory — one place in the framework.
*/
using System;
using Snm.DependencyInjection;
using Snm.Runtime.App.Lifecycle;

namespace Snm.Runtime.App.Hosting
{
    public sealed class ManagedScope : IManagedScope
    {
        private readonly RuntimeContainer _scope;
        private readonly LifecycleService _lifecycle;

        public ManagedScope(RuntimeContainer scope)
        {
            _scope = scope;
            _lifecycle = new LifecycleService(scope);
            _lifecycle.Initialize();
            _lifecycle.Start();
        }

        public T Resolve<T>(string id = null) where T : class
            => _scope.Resolve<T>(id);

        public T[] ResolveAll<T>() where T : class
            => _scope.ResolveAll<T>();

        public void Dispose()
        {
            _lifecycle.Stop();
            _scope.Dispose();
        }
    }
}

using System;
using Snm.DependencyInjection;

namespace Snm.Runtime.App.Hosting
{
    public sealed class AppScopeFactory : IScopeFactory
    {
        private readonly RuntimeContainer _container;

        public AppScopeFactory(RuntimeContainer container)
        {
            _container = container;
        }

        public IManagedScope CreateScope(Action<IBindingContext> configure)
        {
            var scope = _container.CreateScope(configure);
            return new ManagedScope(scope);
        }
    }
}

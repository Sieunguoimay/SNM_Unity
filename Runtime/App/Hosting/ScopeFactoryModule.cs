using Snm.Runtime.App.Composition;
using Snm.DependencyInjection;

namespace Snm.Runtime.App.Hosting
{
    public sealed class ScopeFactoryModule : IAppModule
    {
        void IAppModule.Configure(IBindingContext context)
        {
            context.Bind<IScopeFactory>()
                .ToFactory(r => new AppScopeFactory((RuntimeContainer)r))
                .AsSingleton();
        }
    }
}

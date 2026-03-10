using Snm.Runtime.App.Composition;
using Snm.DependencyInjection;
using Snm.Runtime.App.Lifecycle;

namespace Snm.Runtime.App.Hosting
{
    public sealed class LifecycleServiceModule : IAppModule
    {
        void IAppModule.Configure(IBindingContext context)
        {
            context.Bind<LifecycleService>()
                .ToSingleton(r => new LifecycleService(r));
        }
    }
}
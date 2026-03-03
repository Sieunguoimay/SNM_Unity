using Snm.App.Composition;
using Snm.App.DependencyInjection;
using Snm.App.Lifecycle;

namespace Snm.App.Hosting
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
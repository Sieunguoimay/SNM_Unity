using Snm.Runtime.App.Composition;
using Snm.DependencyInjection;
using Snm.Runtime.App.Lifecycle;

namespace Snm.Runtime.App.Hosting
{
    public class AppComposition
    {
        public static AppHost Compose(IAppModule[] modules)
        {
            var builder = new ContainerBuilder();

            foreach (var module in modules)
            {
                module.Configure(builder);
            }

            IScope rootScope = null;
            builder.Bind<IScope>().ToFactory(_ => rootScope).AsScoped();

            rootScope = builder.Build();

            var lifecycle = rootScope.Resolver.Resolve<LifecycleService>();

            return new AppHost(lifecycle, rootScope);
        }
    }
}

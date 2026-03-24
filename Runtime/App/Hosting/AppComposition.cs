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
            
            var container = builder.Build();
            var lifecycle = container.Resolve<LifecycleService>();

            return new AppHost(lifecycle, container);
        }
    }
}

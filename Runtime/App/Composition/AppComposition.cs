using Snm.App.DependencyInjection;
using Snm.App.Runtime;

namespace Snm.App.Composition
{
    public class AppComposition
    {
        public static AppHost Compose(IAppModuleProvider registry)
        {
            var builder = new ContainerBuilder();

            ConfigureModules(builder, registry);

            var container = builder.Build();

            return new AppHost(resolver: container);
        }

        private static void ConfigureModules(
            ContainerBuilder builder,
            IAppModuleProvider registry)
        {
            foreach (var module in registry.GetModules())
            {
                module.Configure(builder);
            }
        }
    }
}

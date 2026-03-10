using Snm.Runtime.App.Composition;
using Snm.DependencyInjection;

namespace Snm.Runtime.App.Hosting
{
    public class AppComposition
    {
        public static AppHost Compose(IAppModule[] modules)
        {
            var builder = new ContainerBuilder();

            ConfigureModules(builder, modules);

            var container = builder.Build();

            return new AppHost(container);
        }

        private static void ConfigureModules(
            ContainerBuilder builder,
            IAppModule[] modules)
        {
            foreach (var module in modules)
            {
                module.Configure(builder);
            }
        }
    }
}

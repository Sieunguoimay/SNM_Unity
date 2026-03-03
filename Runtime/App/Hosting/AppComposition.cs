using Snm.App.Composition;
using Snm.App.DependencyInjection;

namespace Snm.App.Hosting
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

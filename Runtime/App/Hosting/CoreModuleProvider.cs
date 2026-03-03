using Snm.App.Composition;

namespace Snm.App.Hosting
{
    public class CoreModuleProvider : IAppModuleProvider
    {
        private readonly IAppModule[] coreModules = new[]
        {
            new LifecycleServiceModule()
        };

        public IAppModule[] GetModules()
        {
            return coreModules;
        }
    }
}

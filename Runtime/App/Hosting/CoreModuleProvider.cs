using Snm.Runtime.App.Composition;

namespace Snm.Runtime.App.Hosting
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

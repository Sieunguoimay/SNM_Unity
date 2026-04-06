using Snm.Runtime.App.Composition;

namespace Snm.Runtime.App.Hosting
{
    public class CoreModuleProvider : IAppModuleProvider
    {
        private readonly IAppModule[] coreModules = new IAppModule[]
        {
            new LifecycleServiceModule(),
            new ScopeFactoryModule()
        };

        public IAppModule[] GetModules()
        {
            return coreModules;
        }
    }
}

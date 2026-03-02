using UnityEngine;

namespace Snm.App.Composition
{
    public class AppModulesAsset : ScriptableObject, IAppModuleProvider
    {
        public AppModuleAsset[] modules;

        public IAppModule[] GetModules()
        {
            return modules;
        }
    }
}

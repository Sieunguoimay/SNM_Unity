using UnityEngine;

namespace Snm.Runtime.App.Composition
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

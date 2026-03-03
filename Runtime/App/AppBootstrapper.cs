using System.Collections.Generic;
using System.Linq;
using Snm.App.Composition;
using Snm.App.Hosting;
using UnityEngine;

namespace Snm.App.Unity
{
    public class AppBootstrapper : MonoBehaviour
    {
        [SerializeField] private AppModulesAsset modules;

        private AppHost _appHost;


        private void Start()
        {
            if (modules == null)
            {
                Debug.LogError("AppModulesAsset is not assigned!");
                return;
            }

            try
            {
                var allModules = GetModuleProviders().SelectMany(p => p.GetModules()).ToArray();

                _appHost = AppComposition.Compose(allModules);
                _appHost.Start();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to bootstrap application: {ex.Message}");
                throw;
            }
        }

        private void OnDestroy()
        {
            _appHost?.Stop();
        }

        private IEnumerable<IAppModuleProvider> GetModuleProviders()
        {
            yield return new CoreModuleProvider();
            yield return modules;
        }
    }
}

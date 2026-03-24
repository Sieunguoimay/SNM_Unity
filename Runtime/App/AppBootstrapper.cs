using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.App.Composition;
using Snm.Runtime.App.Hosting;
using UnityEngine;

namespace Snm.Runtime.App.Unity
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
                var allModules = new IAppModuleProvider[] { new CoreModuleProvider(), modules }
                    .SelectMany(p => p.GetModules())
                    .ToArray();

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
            _appHost = null;
        }
    }
}

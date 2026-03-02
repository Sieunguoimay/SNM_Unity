using Snm.App.Composition;
using Snm.App.Runtime;
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
                _appHost = AppComposition.Compose(modules);
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
    }
}

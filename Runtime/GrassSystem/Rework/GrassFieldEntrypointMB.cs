using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassFieldEntrypointMB : MonoBehaviour
    {
        [SerializeField] private GrassSystemConfigSO config;
        [SerializeField] private GrassField grassField;

        private GrassSystemHandle _manager;

        private void OnEnable()
        {
            TryInstall();
        }

        private void OnDisable()
        {
            TryUninstall();
        }

        private void TryUninstall()
        {
            _manager?.DestroySystem();
            _manager = null;
        }

        private void TryInstall()
        {
            if (!Application.IsPlaying(this)) return;
            if (!isActiveAndEnabled) return;
            if (config == null) return;

            _manager ??= new GrassSystemInstaller().Install(config.systemConfig, grassField);
        }

        private void OnValidate()
        {
            TryUninstall();
            TryInstall();
        }
    }
}
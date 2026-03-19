using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassFieldEntrypointMB : MonoBehaviour
    {
        [SerializeField] private GrassSystemConfigSO config;
        [SerializeField] private GrassField grassField;

        private GrassSystemHandle _manager;

        public GrassSystemHandle SystemHandle => _manager;

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

        public void TryInstall()
        {
            if (!Application.IsPlaying(this)) return;
            if (!isActiveAndEnabled) return;
            if (config == null) return;

            _manager ??= new GrassSystemInstaller().Install(config.systemConfig, grassField);
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _manager?.SetDisturbers(disturbers);
        }

        private void OnValidate()
        {
            TryUninstall();
            TryInstall();
        }
    }
}

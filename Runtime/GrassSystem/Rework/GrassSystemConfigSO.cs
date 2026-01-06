using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemConfigSO : ScriptableObject
    {
        [SerializeField] private GrassSystemConfig systemConfig;

        private GrassSystemManager _manager;

        [ContextMenu("Install")]
        private void Install()
        {
            _manager ??= new GrassSystemInstaller().Install(systemConfig);
        }

        [ContextMenu("Uninstall")]
        private void Uninstall()
        {
            _manager?.DestroySystem();
            _manager = null;
        }
    }
}
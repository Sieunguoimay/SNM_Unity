using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{

    public class GrassSystemConfigSO : ScriptableObject
    {
        public GrassSystemConfig systemConfig;

        [NonSerialized]
        private GrassSystemHandle _manager;

        private void OnEnable()
        {
            Debug.Log("OnEnable");
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");
            Uninstall();
        }

        [ContextMenu("Install")]
        private void Install()
        {
            _manager ??= new GrassSystemInstaller().Install(systemConfig, null);
        }

        [ContextMenu("Uninstall")]
        private void Uninstall()
        {
            try
            {
                _manager?.DestroySystem();
            }
            catch { }

            _manager = null;
        }

        [ContextMenu("Open Debug Tool")]
        private void OpenDebugTool()
        {
            _manager?.Editor_OpenDebugWindow();
        }
    }
}
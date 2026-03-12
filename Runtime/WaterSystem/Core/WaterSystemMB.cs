using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemMB : MonoBehaviour
    {
        [SerializeField] private WaterConfig config;
        [SerializeField] private Camera sourceCamera;

        private WaterSystemHandle _handle;

        private void Awake()
        {
            if (!isActiveAndEnabled) return;
            if (!sourceCamera) sourceCamera = Camera.main;
        }

        private void Start() => Setup();
        private void OnDestroy() => Teardown();

        [ContextMenu("Setup")]
        private void Setup()
        {
            if (!isActiveAndEnabled) return;
            if (!ValidateConfig()) return;

            _handle = WaterSystemInstaller.Install(config, sourceCamera);
        }

        [ContextMenu("Teardown")]
        private void Teardown()
        {
            _handle?.Dispose();
            _handle = null;
        }

        private bool ValidateConfig()
        {
            if (sourceCamera == null)
            {
                Debug.LogError("[WaterSystem] Source camera is not assigned.", this);
                return false;
            }
            return config.surface.waterSurfaceShader != null || config.surface.waterSurfaceMaterial != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(config.surface.size.x, 0, config.surface.size.y));
        }

        [ContextMenu("Auto-assign config references")]
        private void AutoAssignConfigReferences() => WaterSystemTestWindow.AutoAssignConfigReferences(config);
#endif
    }
}
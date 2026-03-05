using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemEntrypointMB : MonoBehaviour
    {
        [SerializeField] private WaterConfig config;

        private WaterSystemHandle _handle;

        private void Start() => Setup();
        private void OnDestroy() => Teardown();

        [ContextMenu("Setup")]
        private void Setup()
        {
            if (!isActiveAndEnabled) return;
            if (ValidateConfig()) return;

            _handle = WaterSystemInstaller.Install(gameObject, config, Camera.main);
        }

        [ContextMenu("Teardown")]
        private void Teardown()
        {
            _handle?.Dispose();
            _handle = null;
        }

        private bool ValidateConfig()
        {
            return config.waterSurfaceShader == null && config.waterSurfaceMaterial == null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(config.waterSurfaceSize.x, 0, config.waterSurfaceSize.y));
        }
#endif
    }
}
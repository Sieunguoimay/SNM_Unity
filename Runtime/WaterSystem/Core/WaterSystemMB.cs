using System.Collections.Generic;
using Snm.WaterSystem.Buoyancy;
using Snm.WaterSystem.Wave;
using UnityEngine;

namespace Snm.WaterSystem
{
    public class WaterSystemMB : MonoBehaviour
    {
        [SerializeField] private WaterConfig config;
        [SerializeField] private Camera sourceCamera;

        private WaterSystemHandle _handle;
        private IEnumerable<IWaveDisturber> _disturbers;
        private IEnumerable<IBuoyant> _buoyants;

        /// <summary>
        /// The active water system handle. Available after <see cref="Start"/> has run.
        /// </summary>
        public WaterSystemHandle Handle => _handle;

        /// <summary>
        /// Provides a live disturber source before Start() runs.
        /// Call this immediately after the prefab is instantiated (e.g. from WaterEntity.OnInitialized).
        /// The list is held by reference and re-enumerated each frame, so registration changes
        /// made to the source list are picked up automatically.
        /// </summary>
        public void SetDisturbers(IEnumerable<IWaveDisturber> disturbers)
        {
            _disturbers = disturbers;
        }

        /// <summary>
        /// Provides a live buoyant source before Start() runs.
        /// Same pattern as <see cref="SetDisturbers"/>.
        /// </summary>
        public void SetBuoyants(IEnumerable<IBuoyant> buoyants)
        {
            _buoyants = buoyants;
        }

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

            _handle = WaterSystemFactory.Create(config, sourceCamera, _disturbers, _buoyants, transform);
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

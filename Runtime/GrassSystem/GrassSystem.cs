using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class GrassSystem : MonoBehaviour
    {
        [SerializeField] GrassSystemConfig config = new();

        GrassSystemHandle _handle;
        IReadOnlyList<IGrassDisturber> _disturbers;
        GrassGridBuilder.Result _grid;

        public GrassSystemConfig Config => config;
        public GrassRenderer Renderer => _handle?.Renderer;
        public GrassTrample Trample => _handle?.Trample;
        public int InstanceCount => _grid.Matrices?.Length ?? 0;
        public Matrix4x4[] Matrices => _grid.Matrices;
        public SurfaceCanvas Canvas => _handle?.Canvas;

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _disturbers = disturbers;
            _handle?.Trample?.SetDisturbers(disturbers);
        }

        public void Rebuild()
        {
            OnDisable();
            OnEnable();
        }

        void OnValidate()
        {
            UpdateGrassHeightFromMesh();
        }

        void OnEnable()
        {
            if (!config.HasLayers && (config.grassMesh == null || config.grassMaterial == null)) return;
            if (config.HasLayers && !ValidateLayers()) return;

            if (config.HasLayers && config.placementMap == null)
                Debug.LogWarning("[GrassSystem] Layers are configured but no placement map is assigned. " +
                                 "All layers will place grass at every cell (no channel-based filtering).", this);

            _grid = GrassGridBuilder.Build(config, transform);

            _handle = GrassSystemFactory.Create(config, _grid.Matrices, _grid.LayerMatrices, _grid.Canvas, _grid.WorldBounds);

            if (_disturbers != null)
                _handle?.Trample?.SetDisturbers(_disturbers);
        }

        void OnDisable()
        {
            _handle?.Dispose();
            _handle = null;
        }

        bool ValidateLayers()
        {
            foreach (var layer in config.layers)
            {
                if (layer.mesh == null || layer.material == null) return false;
            }
            return true;
        }

        public void UpdateGrassHeightFromMesh()
        {
            if (config.grassMesh != null)
            {
                config.bladeHeight = config.grassMesh.bounds.size.y;
            }
        }
    }
}

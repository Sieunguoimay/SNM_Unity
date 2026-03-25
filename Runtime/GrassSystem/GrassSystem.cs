using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class GrassSystem : MonoBehaviour
    {
        [SerializeField] GrassSystemConfig config = new();

        GrassSystemHandle _handle;

        Matrix4x4[] _matrices;
        Matrix4x4[][] _layerMatrices;
        SurfaceCanvas _canvas;
        Bounds _worldBounds;

        public GrassSystemConfig Config => config;
        public GrassRenderer Renderer => _handle?.Renderer;
        public GrassTrample Trample => _handle?.Trample;
        public int InstanceCount => _matrices?.Length ?? 0;
        public Matrix4x4[] Matrices => _matrices;
        public SurfaceCanvas Canvas => _handle?.Canvas;

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _handle?.Trample?.SetDisturbers(disturbers);
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

            BuildGrid();

            _handle = GrassSystemFactory.Create(config, _matrices, _layerMatrices, _canvas, _worldBounds);
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

        void BuildGrid()
        {
            int sizeX, sizeZ;

            if (config.placementMap != null)
            {
                sizeX = config.placementMap.width;
                sizeZ = config.placementMap.height;
            }
            else
            {
                sizeX = config.gridSize.x;
                sizeZ = config.gridSize.y;
            }

            float spacingX = config.cellSpacing.x;
            float spacingZ = config.cellSpacing.y;

            float totalWidth = (sizeX - 1) * spacingX;
            float totalDepth = (sizeZ - 1) * spacingZ;
            Vector3 pivotOffset = new(-totalWidth * 0.5f, 0f, -totalDepth * 0.5f);

            // Build canvas and bounds (same for all layers)
            var worldCenter = transform.TransformPoint(pivotOffset + new Vector3(totalWidth * 0.5f, 0f, totalDepth * 0.5f));
            var worldCorner = transform.TransformPoint(pivotOffset + new Vector3(totalWidth, 0f, totalDepth));
            var worldOrigin = transform.TransformPoint(pivotOffset);
            var worldSize = new Vector2(
                Mathf.Abs(worldCorner.x - worldOrigin.x),
                Mathf.Abs(worldCorner.z - worldOrigin.z));

            _canvas = new SurfaceCanvas
            {
                Position = worldCenter,
                Rotation = Quaternion.identity,
                Size = worldSize
            };
            _worldBounds = new Bounds(worldCenter, new Vector3(worldSize.x, 10f, worldSize.y));

            Color32[] pixels = config.placementMap != null
                ? config.placementMap.GetPixels32()
                : null;

            if (config.HasLayers)
            {
                _layerMatrices = new Matrix4x4[config.layers.Length][];
                for (int i = 0; i < config.layers.Length; i++)
                {
                    _layerMatrices[i] = BuildLayerGrid(
                        sizeX, sizeZ, spacingX, spacingZ, pivotOffset, pixels,
                        config.layers[i]);
                }
                // Primary matrices = first layer (for backward compat)
                _matrices = _layerMatrices.Length > 0 ? _layerMatrices[0] : System.Array.Empty<Matrix4x4>();
            }
            else
            {
                _matrices = BuildDefaultGrid(sizeX, sizeZ, spacingX, spacingZ, pivotOffset, pixels);
                _layerMatrices = null;
            }
        }

        Matrix4x4[] BuildDefaultGrid(
            int sizeX, int sizeZ, float spacingX, float spacingZ,
            Vector3 pivotOffset, Color32[] pixels)
        {
            var list = new List<Matrix4x4>(sizeX * sizeZ);

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float density = 1f;
                    float yawNorm = -1f; // sentinel: use hash
                    float scaleNorm = 0.5f;

                    if (pixels != null)
                    {
                        var c = pixels[z * sizeX + x];
                        density = c.r / 255f;
                        if (density < config.densityThreshold) continue;
                        yawNorm = c.g / 255f;
                        scaleNorm = c.b / 255f;
                    }

                    var localPos = new Vector3(x * spacingX, 0f, z * spacingZ) + pivotOffset;
                    var worldPos = transform.TransformPoint(localPos);

                    float yaw;
                    if (yawNorm >= 0f)
                        yaw = yawNorm * 360f;
                    else
                    {
                        uint hash = (uint)(x * 73856093 ^ z * 19349663);
                        yaw = (hash % 3600) / 10f;
                    }

                    var rot = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    float scale = pixels != null
                        ? Mathf.Lerp(config.minScale, config.maxScale, scaleNorm)
                        : 1f;

                    list.Add(Matrix4x4.TRS(worldPos, rot, Vector3.one * scale));
                }
            }

            return list.ToArray();
        }

        Matrix4x4[] BuildLayerGrid(
            int sizeX, int sizeZ, float spacingX, float spacingZ,
            Vector3 pivotOffset, Color32[] pixels,
            GrassLayerConfig layer)
        {
            var list = new List<Matrix4x4>(sizeX * sizeZ);

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (pixels != null)
                    {
                        var c = pixels[z * sizeX + x];
                        float density = GetChannel(c, layer.densityChannel) / 255f;
                        if (density < layer.densityThreshold) continue;
                    }

                    var localPos = new Vector3(x * spacingX, 0f, z * spacingZ) + pivotOffset;
                    var worldPos = transform.TransformPoint(localPos);

                    // Deterministic yaw varied per layer
                    uint hash = (uint)(x * 73856093 ^ z * 19349663 ^ (int)(layer.yawRandomSeed * 7919));
                    float yaw = (hash % 3600) / 10f;
                    var rot = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    // Scale from hash (deterministic per-cell per-layer)
                    uint scaleHash = (uint)(x * 45161 ^ z * 37913 ^ (int)(layer.yawRandomSeed * 12289));
                    float scaleNorm = (scaleHash % 1000) / 999f;
                    float scale = Mathf.Lerp(layer.minScale, layer.maxScale, scaleNorm);

                    list.Add(Matrix4x4.TRS(worldPos, rot, Vector3.one * scale));
                }
            }

            return list.ToArray();
        }

        static float GetChannel(Color32 c, int channel)
        {
            return channel switch
            {
                0 => c.r,
                1 => c.g,
                2 => c.b,
                3 => c.a,
                _ => c.r
            };
        }
    }
}

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
            if (config.grassMesh == null || config.grassMaterial == null) return;

            BuildGrid();

            _handle = GrassSystemFactory.Create(config, _matrices, _canvas, _worldBounds);
        }

        void OnDisable()
        {
            _handle?.Dispose();
            _handle = null;
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
            int sizeX = config.gridSize.x;
            int sizeZ = config.gridSize.y;
            float spacingX = config.cellSpacing.x;
            float spacingZ = config.cellSpacing.y;

            float totalWidth = (sizeX - 1) * spacingX;
            float totalDepth = (sizeZ - 1) * spacingZ;

            // Pivot at center of grid
            Vector3 pivotOffset = new(-totalWidth * 0.5f, 0f, -totalDepth * 0.5f);

            _matrices = new Matrix4x4[sizeX * sizeZ];
            int index = 0;

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    var localPos = new Vector3(x * spacingX, 0f, z * spacingZ) + pivotOffset;
                    var worldPos = transform.TransformPoint(localPos);

                    // Deterministic random rotation per blade
                    uint hash = (uint)(x * 73856093 ^ z * 19349663);
                    float yaw = (hash % 3600) / 10f; // 0–360 degrees
                    var rot = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    _matrices[index++] = Matrix4x4.TRS(worldPos, rot, Vector3.one);
                }
            }

            // Build SurfaceCanvas
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
        }
    }
}

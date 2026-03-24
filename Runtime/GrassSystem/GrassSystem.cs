using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class GrassSystem : MonoBehaviour
    {
        [SerializeField] GrassSystemConfig config = new();

        GrassRenderer _renderer;
        GrassTrample _trample;
        IReadOnlyList<IGrassDisturber> _disturbers = new List<IGrassDisturber>();

        Matrix4x4[] _matrices;
        SurfaceCanvas _canvas;
        Bounds _worldBounds;

        public GrassSystemConfig Config => config;
        public GrassRenderer Renderer => _renderer;
        public GrassTrample Trample => _trample;
        public int InstanceCount => _matrices?.Length ?? 0;
        public SurfaceCanvas Canvas => _canvas;

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            _disturbers = disturbers ?? new List<IGrassDisturber>();
        }

        void OnEnable()
        {
            if (config.grassMesh == null || config.grassMaterial == null) return;

            BuildGrid();

            var worldMin = _canvas.WorldMin;
            var canvasVec = new Vector4(worldMin.x, worldMin.y, _canvas.Size.x, _canvas.Size.y);

            _renderer = new GrassRenderer();
            _renderer.Setup(config.grassMesh, config.grassMaterial, _matrices, _worldBounds);
            _renderer.SetWorldCanvas(canvasVec);

            if (config.windMap != null)
                _renderer.SetWind(config.windMap, config.windStrength, config.windScrollSpeed, config.windMapScale);

            if (config.trampleEnabled && config.trampleShader != null)
            {
                _trample = new GrassTrample();
                _trample.Setup(config, _canvas);
                _renderer.SetTrampleMap(_trample.OutputTexture);
            }
        }

        void Update()
        {
            if (_trample != null)
            {
                _trample.Update(_disturbers, Time.deltaTime);
            }

            _renderer?.Render();
        }

        void OnDisable()
        {
            _renderer?.Dispose();
            _renderer = null;
            _trample?.Dispose();
            _trample = null;
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

using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    [ExecuteInEditMode]
    public class WaterSurfaceMB : MonoBehaviour
    {
        private SurfaceCanvas _canvas;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        public SurfaceCanvas Canvas => _canvas;

        public void Setup(Mesh mesh, Material material, Vector2 size)
        {
            _canvas = new SurfaceCanvas
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Size = size
            };

            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshFilter.sharedMesh = mesh;

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = material;
        }

        private void Update()
        {
            if (_canvas == null) return;
            _canvas.Position = transform.position;
            _canvas.Rotation = transform.rotation;
        }
    }
}

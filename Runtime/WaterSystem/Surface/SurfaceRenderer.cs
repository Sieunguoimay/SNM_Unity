// ═══════════════════════════════════════════════════════════════
// WaterSurfaceRenderer.cs
// Creates and owns the MeshRenderer GameObject for the water quad.
// Applies material property updates each frame via the binder.
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    public class SurfaceRenderer : IUpdateTarget
    {
        private readonly SurfaceData _surface;
        private readonly GameObject   _gameObject;

        public SurfaceRenderer(SurfaceData surface, Material material)
        {
            _surface  = surface;

            var go = new GameObject("[WaterSurface]");
            go.AddComponent<MeshFilter>().sharedMesh      = surface.mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            _gameObject = go;
        }

        public void Update()
        {
            _gameObject.transform.SetPositionAndRotation(_surface.position, _surface.rotation);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_gameObject);
        }
    }
}

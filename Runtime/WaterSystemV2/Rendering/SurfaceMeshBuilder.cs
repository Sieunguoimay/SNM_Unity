using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>Fallback flat quad in the local XZ plane (Y up).</summary>
    public static class SurfaceMeshBuilder
    {
        public static Mesh CreateQuad(Vector2 size)
        {
            float hx = size.x * 0.5f;
            float hz = size.y * 0.5f;

            var mesh = new Mesh
            {
                name = "WaterSurfaceQuad",
                vertices = new Vector3[]
                {
                    new(-hx, 0f, -hz),
                    new(-hx, 0f,  hz),
                    new( hx, 0f,  hz),
                    new( hx, 0f, -hz),
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new Vector2[]
                {
                    new(0, 0),
                    new(0, 1),
                    new(1, 1),
                    new(1, 0),
                },
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public static class WaterSurfaceMeshBuilder
    {
        public static Mesh CreateQuadMesh(Vector2 size)
        {
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;

            // Local quad in XZ plane (Y up)
            Vector3[] verts = new Vector3[4]
            {
                new(-hx, 0f, -hy),
                new(-hx, 0f,  hy),
                new( hx, 0f,  hy),
                new( hx, 0f, -hy),
            };

            int[] tris = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            Vector2[] uvs = new Vector2[4]
            {
                new(0, 0),
                new(0, 1),
                new(1, 1),
                new(1, 0),
            };

            var mesh = new Mesh
            {
                name = "WaterSurfaceQuad",
                vertices = verts,
                triangles = tris,
                uv = uvs
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
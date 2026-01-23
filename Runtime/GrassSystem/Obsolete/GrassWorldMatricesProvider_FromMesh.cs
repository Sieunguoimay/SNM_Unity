#if UNITY_EDITOR
#endif
using UnityEngine;

namespace Snm.Runtime.GrassSystem.Obsolete
{
    public class GrassWorldMatricesProvider_FromMesh
    {
        private readonly Mesh mesh;

        public GrassWorldMatricesProvider_FromMesh(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public Matrix4x4[] GetWorldMatrices(Vector3 scale, Matrix4x4 localToWorld)
        {
            if (mesh == null)
                return System.Array.Empty<Matrix4x4>();

            var vertices = mesh.vertices;
            var normals = mesh.normals;

            return GetWorldMatrices(vertices, normals, scale, localToWorld);
        }

        public static Matrix4x4[] GetWorldMatrices(Vector3[] vertices, Vector3[] normals, Vector3 scale, Matrix4x4 localToWorld)
        {
            var count = vertices.Length;
            var matrices = new Matrix4x4[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 position = vertices[i];

                // Fallback if mesh has no normals
                Vector3 normal = (normals != null && normals.Length == count)
                    ? normals[i]
                    : Vector3.up;

                // Rotate grass so its up-axis follows the surface normal
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

                matrices[i] = localToWorld * Matrix4x4.TRS(
                    position,
                    rotation,
                    scale
                );
            }

            return matrices;
        }
    }
}
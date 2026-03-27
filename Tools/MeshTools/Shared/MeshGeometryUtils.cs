#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Tools.MeshTools
{
    public static class MeshGeometryUtils
    {
        #region Ray Intersection

        public static bool RayTriangleIntersection(
            Ray ray, Vector3 v0, Vector3 v1, Vector3 v2,
            out float t, out Vector3 barycentricCoord)
        {
            t = 0f;
            barycentricCoord = Vector3.zero;

            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -1e-6f && a < 1e-6f) return false;

            float f = 1f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f) return false;

            t = f * Vector3.Dot(e2, q);
            if (t < 1e-6f) return false;

            barycentricCoord = new Vector3(1f - u - v, u, v);
            return true;
        }

        public static bool RayMeshIntersection(
            Ray ray, Vector3[] vertices, int[] triangles, Matrix4x4 localToWorld,
            out int hitTriIndex, out float hitDistance, out Vector3 hitBary)
        {
            hitTriIndex = -1;
            hitDistance = float.MaxValue;
            hitBary = Vector3.zero;

            // Transform ray to local space
            Matrix4x4 worldToLocal = localToWorld.inverse;
            Vector3 localOrigin = worldToLocal.MultiplyPoint3x4(ray.origin);
            Vector3 localDir = worldToLocal.MultiplyVector(ray.direction).normalized;
            Ray localRay = new(localOrigin, localDir);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length) continue;

                if (RayTriangleIntersection(localRay, vertices[i0], vertices[i1], vertices[i2],
                        out float t, out Vector3 bary))
                {
                    if (t < hitDistance)
                    {
                        hitDistance = t;
                        hitTriIndex = i / 3;
                        hitBary = bary;
                    }
                }
            }

            return hitTriIndex >= 0;
        }

        #endregion

        #region Normals & Tangents

        public static Vector3[] ComputeAreaWeightedNormals(Vector3[] vertices, int[] triangles)
        {
            var normals = new Vector3[vertices.Length];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length) continue;

                Vector3 e1 = vertices[i1] - vertices[i0];
                Vector3 e2 = vertices[i2] - vertices[i0];
                Vector3 faceNormal = Vector3.Cross(e1, e2); // magnitude = 2 * area

                normals[i0] += faceNormal;
                normals[i1] += faceNormal;
                normals[i2] += faceNormal;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].sqrMagnitude > 1e-8f)
                    normals[i].Normalize();
                else
                    normals[i] = Vector3.up;
            }

            return normals;
        }

        public static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).normalized;
        }

        public static Vector4[] ComputeTangents(
            Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            int vertCount = vertices.Length;
            var tan1 = new Vector3[vertCount];
            var tan2 = new Vector3[vertCount];
            var tangents = new Vector4[vertCount];

            if (uvs == null || uvs.Length != vertCount)
            {
                for (int i = 0; i < vertCount; i++)
                    tangents[i] = new Vector4(1, 0, 0, 1);
                return tangents;
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= vertCount || i1 >= vertCount || i2 >= vertCount) continue;

                Vector3 v1 = vertices[i1] - vertices[i0];
                Vector3 v2 = vertices[i2] - vertices[i0];
                Vector2 w1 = uvs[i1] - uvs[i0];
                Vector2 w2 = uvs[i2] - uvs[i0];

                float r = w1.x * w2.y - w2.x * w1.y;
                if (Mathf.Abs(r) < 1e-8f) continue;
                r = 1f / r;

                Vector3 sdir = new(
                    (w2.y * v1.x - w1.y * v2.x) * r,
                    (w2.y * v1.y - w1.y * v2.y) * r,
                    (w2.y * v1.z - w1.y * v2.z) * r);
                Vector3 tdir = new(
                    (w1.x * v2.x - w2.x * v1.x) * r,
                    (w1.x * v2.y - w2.x * v1.y) * r,
                    (w1.x * v2.z - w2.x * v1.z) * r);

                tan1[i0] += sdir; tan1[i1] += sdir; tan1[i2] += sdir;
                tan2[i0] += tdir; tan2[i1] += tdir; tan2[i2] += tdir;
            }

            for (int i = 0; i < vertCount; i++)
            {
                Vector3 n = normals[i];
                Vector3 t = tan1[i];

                // Gram-Schmidt orthogonalize
                Vector3 tangent = (t - n * Vector3.Dot(n, t)).normalized;
                float w = Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0f ? -1f : 1f;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, w);
            }

            return tangents;
        }

        #endregion

        #region Area & Bounds

        public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
        }

        public static float TriangleArea2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
        }

        public static Bounds ComputeBounds(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 min = vertices[0], max = vertices[0];
            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        #endregion

        #region Transform

        public static Vector3[] TransformVertices(Vector3[] vertices, Matrix4x4 matrix)
        {
            var result = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                result[i] = matrix.MultiplyPoint3x4(vertices[i]);
            return result;
        }

        public static Vector3[] TransformNormals(Vector3[] normals, Matrix4x4 matrix)
        {
            // Use inverse transpose for normal transformation
            Matrix4x4 normalMatrix = matrix.inverse.transpose;
            var result = new Vector3[normals.Length];
            for (int i = 0; i < normals.Length; i++)
                result[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            return result;
        }

        #endregion

        #region Distance

        public static float PointToSegmentDistanceSq(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
            Vector3 closest = a + t * ab;
            return (point - closest).sqrMagnitude;
        }

        public static float PointToSegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt(PointToSegmentDistanceSq(point, a, b));
        }

        #endregion
    }
}
#endif

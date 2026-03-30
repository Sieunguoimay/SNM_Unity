#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public class QuadricErrorDecimator
    {
        // Symmetric 4x4 matrix stored as 10 floats (upper triangle)
        struct Quadric
        {
            public double a00, a01, a02, a03;
            public double a11, a12, a13;
            public double a22, a23;
            public double a33;

            public static Quadric FromPlane(Vector3 normal, float d)
            {
                double a = normal.x, b = normal.y, c = normal.z;
                return new Quadric
                {
                    a00 = a * a, a01 = a * b, a02 = a * c, a03 = a * d,
                    a11 = b * b, a12 = b * c, a13 = b * d,
                    a22 = c * c, a23 = c * d,
                    a33 = d * d
                };
            }

            public static Quadric operator +(Quadric a, Quadric b) => new()
            {
                a00 = a.a00 + b.a00, a01 = a.a01 + b.a01, a02 = a.a02 + b.a02, a03 = a.a03 + b.a03,
                a11 = a.a11 + b.a11, a12 = a.a12 + b.a12, a13 = a.a13 + b.a13,
                a22 = a.a22 + b.a22, a23 = a.a23 + b.a23,
                a33 = a.a33 + b.a33
            };

            public double Evaluate(Vector3 v)
            {
                double x = v.x, y = v.y, z = v.z;
                return a00 * x * x + 2 * a01 * x * y + 2 * a02 * x * z + 2 * a03 * x
                     + a11 * y * y + 2 * a12 * y * z + 2 * a13 * y
                     + a22 * z * z + 2 * a23 * z
                     + a33;
            }

            public bool TryOptimalPosition(out Vector3 result)
            {
                // Solve the 3x3 linear system for minimum error point
                // | a00 a01 a02 | |x|   |-a03|
                // | a01 a11 a12 | |y| = |-a13|
                // | a02 a12 a22 | |z|   |-a23|
                double det = a00 * (a11 * a22 - a12 * a12)
                           - a01 * (a01 * a22 - a12 * a02)
                           + a02 * (a01 * a12 - a11 * a02);

                result = Vector3.zero;
                if (System.Math.Abs(det) < 1e-10) return false;

                double invDet = 1.0 / det;
                result = new Vector3(
                    (float)(invDet * (-(a03 * (a11 * a22 - a12 * a12) - a01 * (a13 * a22 - a12 * a23) + a02 * (a13 * a12 - a11 * a23)))),
                    (float)(invDet * (-(a00 * (a13 * a22 - a12 * a23) - a03 * (a01 * a22 - a12 * a02) + a02 * (a01 * a23 - a13 * a02)))),
                    (float)(invDet * (-(a00 * (a11 * a23 - a13 * a12) - a01 * (a01 * a23 - a13 * a02) + a03 * (a01 * a12 - a11 * a02))))
                );
                return true;
            }

            public static Quadric operator *(Quadric q, double s) => new()
            {
                a00 = q.a00 * s, a01 = q.a01 * s, a02 = q.a02 * s, a03 = q.a03 * s,
                a11 = q.a11 * s, a12 = q.a12 * s, a13 = q.a13 * s,
                a22 = q.a22 * s, a23 = q.a23 * s,
                a33 = q.a33 * s
            };
        }

        struct EdgeCollapse
        {
            public int V0, V1;
            public double Cost;
            public Vector3 OptimalPos;
        }

        public struct DecimationSettings
        {
            public int TargetTriangleCount;
            public float BoundaryPenalty;
            public bool PreserveBoundary;
            public bool PreserveUVSeams;
        }

        public static Mesh Decimate(Mesh sourceMesh, DecimationSettings settings)
        {
            var em = EditableMesh.FromMesh(sourceMesh);
            int targetTris = Mathf.Max(4, settings.TargetTriangleCount);

            if (em.TriangleCount <= targetTris)
                return Object.Instantiate(sourceMesh);

            int vertCount = em.VertexCount;
            var positions = (Vector3[])em.Positions.Clone();
            var triangles = new List<int>(em.Triangles);
            var quadrics = new Quadric[vertCount];
            var deleted = new bool[vertCount];

            // Step 1: Compute per-vertex quadrics from adjacent face planes
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                Vector3 normal = MeshGeometryUtils.ComputeFaceNormal(positions[i0], positions[i1], positions[i2]);
                float d = -Vector3.Dot(normal, positions[i0]);
                float area = MeshGeometryUtils.TriangleArea(positions[i0], positions[i1], positions[i2]);
                Quadric q = Quadric.FromPlane(normal, d) * area;

                quadrics[i0] = quadrics[i0] + q;
                quadrics[i1] = quadrics[i1] + q;
                quadrics[i2] = quadrics[i2] + q;
            }

            // Boundary penalty
            HashSet<long> boundaryEdges = null;
            if (settings.PreserveBoundary)
            {
                boundaryEdges = new HashSet<long>();
                var edgeFaceCount = new Dictionary<long, int>();
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    AddEdgeCount(edgeFaceCount, triangles[i], triangles[i + 1]);
                    AddEdgeCount(edgeFaceCount, triangles[i + 1], triangles[i + 2]);
                    AddEdgeCount(edgeFaceCount, triangles[i + 2], triangles[i]);
                }
                foreach (var kvp in edgeFaceCount)
                    if (kvp.Value == 1) boundaryEdges.Add(kvp.Key);
            }

            // UV seam detection
            HashSet<int> uvSeamVerts = null;
            if (settings.PreserveUVSeams && em.UVChannels[0] != null)
            {
                uvSeamVerts = DetectUVSeamVertices(em);
            }

            // Step 2: Build edge collapse priority queue (simple list, re-sorted)
            // For performance on large meshes, a proper min-heap would be better,
            // but this is sufficient for editor use.
            var collapses = new List<EdgeCollapse>();
            var edges = new HashSet<long>();

            for (int i = 0; i < triangles.Count; i += 3)
            {
                TryAddEdge(edges, collapses, triangles[i], triangles[i + 1],
                    positions, quadrics, boundaryEdges, uvSeamVerts, settings.BoundaryPenalty);
                TryAddEdge(edges, collapses, triangles[i + 1], triangles[i + 2],
                    positions, quadrics, boundaryEdges, uvSeamVerts, settings.BoundaryPenalty);
                TryAddEdge(edges, collapses, triangles[i + 2], triangles[i],
                    positions, quadrics, boundaryEdges, uvSeamVerts, settings.BoundaryPenalty);
            }

            collapses.Sort((a, b) => a.Cost.CompareTo(b.Cost));

            // Step 3: Iteratively collapse cheapest edges
            int currentTris = triangles.Count / 3;
            int collapseIdx = 0;

            while (currentTris > targetTris && collapseIdx < collapses.Count)
            {
                var collapse = collapses[collapseIdx++];
                int v0 = collapse.V0, v1 = collapse.V1;

                if (deleted[v0] || deleted[v1]) continue;

                // Collapse: keep v0, delete v1
                positions[v0] = collapse.OptimalPos;
                quadrics[v0] = quadrics[v0] + quadrics[v1];
                deleted[v1] = true;

                // Remap v1 -> v0 in all triangles
                for (int i = 0; i < triangles.Count; i++)
                    if (triangles[i] == v1) triangles[i] = v0;

                // Remove degenerate triangles
                for (int i = triangles.Count - 3; i >= 0; i -= 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    if (a == b || b == c || c == a)
                    {
                        triangles.RemoveRange(i, 3);
                        currentTris--;
                    }
                }
            }

            // Step 4: Rebuild mesh with compacted vertices
            var remap = new int[vertCount];
            var finalPositions = new List<Vector3>();
            for (int i = 0; i < vertCount; i++)
            {
                if (deleted[i])
                {
                    remap[i] = -1;
                    continue;
                }
                remap[i] = finalPositions.Count;
                finalPositions.Add(positions[i]);
            }

            var finalTrisList = new List<int>();
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = remap[triangles[i]], b = remap[triangles[i + 1]], c = remap[triangles[i + 2]];
                if (a < 0 || b < 0 || c < 0) continue;
                if (a == b || b == c || c == a) continue;
                finalTrisList.Add(a); finalTrisList.Add(b); finalTrisList.Add(c);
            }

            var resultMesh = new Mesh { name = sourceMesh.name + "_decimated" };
            if (finalPositions.Count > 65535)
                resultMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            resultMesh.vertices = finalPositions.ToArray();
            resultMesh.triangles = finalTrisList.ToArray();
            resultMesh.RecalculateNormals();
            resultMesh.RecalculateBounds();

            return resultMesh;
        }

        static void AddEdgeCount(Dictionary<long, int> dict, int v0, int v1)
        {
            long key = EditableMesh.EdgeKey(v0, v1);
            dict.TryGetValue(key, out int cnt);
            dict[key] = cnt + 1;
        }

        static void TryAddEdge(HashSet<long> edges, List<EdgeCollapse> collapses,
            int v0, int v1, Vector3[] positions, Quadric[] quadrics,
            HashSet<long> boundaryEdges, HashSet<int> uvSeamVerts, float boundaryPenalty)
        {
            long key = EditableMesh.EdgeKey(v0, v1);
            if (!edges.Add(key)) return;

            Quadric combined = quadrics[v0] + quadrics[v1];
            Vector3 optimalPos;

            if (!combined.TryOptimalPosition(out optimalPos))
                optimalPos = (positions[v0] + positions[v1]) * 0.5f;

            double cost = combined.Evaluate(optimalPos);

            // Boundary penalty
            if (boundaryEdges != null && boundaryEdges.Contains(key))
                cost *= boundaryPenalty > 0 ? boundaryPenalty : 100.0;

            // UV seam penalty
            if (uvSeamVerts != null && (uvSeamVerts.Contains(v0) || uvSeamVerts.Contains(v1)))
                cost *= 50.0;

            collapses.Add(new EdgeCollapse
            {
                V0 = v0 < v1 ? v0 : v1,
                V1 = v0 < v1 ? v1 : v0,
                Cost = cost,
                OptimalPos = optimalPos
            });
        }

        static HashSet<int> DetectUVSeamVertices(EditableMesh mesh)
        {
            var seamVerts = new HashSet<int>();
            if (mesh.UVChannels[0] == null) return seamVerts;

            var uvs = mesh.UVChannels[0];
            // Vertices at the same position but different UVs indicate a seam
            var posToUV = new Dictionary<int, Vector2>();

            for (int i = 0; i < mesh.VertexCount; i++)
            {
                // Find vertices at the same position
                for (int j = i + 1; j < mesh.VertexCount; j++)
                {
                    if ((mesh.Positions[i] - mesh.Positions[j]).sqrMagnitude < 1e-8f)
                    {
                        if (i < uvs.Count && j < uvs.Count &&
                            (uvs[i] - uvs[j]).sqrMagnitude > 1e-6f)
                        {
                            seamVerts.Add(i);
                            seamVerts.Add(j);
                        }
                    }
                }
            }

            return seamVerts;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class NormalsOperations
    {
        public static void RecalculateNormals(EditableMesh mesh)
        {
            mesh.Normals = MeshGeometryUtils.ComputeAreaWeightedNormals(mesh.Positions, mesh.Triangles);
        }

        public static void RecalculateTangents(EditableMesh mesh)
        {
            if (mesh.Normals == null) RecalculateNormals(mesh);
            var uvs = mesh.UVChannels[0]?.ToArray();
            mesh.Tangents = MeshGeometryUtils.ComputeTangents(mesh.Positions, mesh.Normals, uvs, mesh.Triangles);
        }

        public static void FlipNormals(EditableMesh mesh, HashSet<int> vertIndices = null)
        {
            if (mesh.Normals == null) return;

            if (vertIndices == null)
            {
                for (int i = 0; i < mesh.Normals.Length; i++)
                    mesh.Normals[i] = -mesh.Normals[i];
                // Reverse winding
                for (int i = 0; i < mesh.Triangles.Length; i += 3)
                    (mesh.Triangles[i + 1], mesh.Triangles[i + 2]) = (mesh.Triangles[i + 2], mesh.Triangles[i + 1]);
            }
            else
            {
                foreach (int v in vertIndices)
                    if (v < mesh.Normals.Length) mesh.Normals[v] = -mesh.Normals[v];
            }
        }

        public static void SmoothNormals(EditableMesh mesh, float distanceThreshold, HashSet<int> vertIndices = null)
        {
            if (mesh.Normals == null) return;
            float threshSq = distanceThreshold * distanceThreshold;

            var targets = vertIndices ?? new HashSet<int>();
            if (vertIndices == null)
                for (int i = 0; i < mesh.VertexCount; i++) targets.Add(i);

            var newNormals = (Vector3[])mesh.Normals.Clone();

            foreach (int v in targets)
            {
                Vector3 sum = mesh.Normals[v];
                int count = 1;

                for (int j = 0; j < mesh.VertexCount; j++)
                {
                    if (j == v) continue;
                    if ((mesh.Positions[v] - mesh.Positions[j]).sqrMagnitude <= threshSq)
                    {
                        sum += mesh.Normals[j];
                        count++;
                    }
                }

                newNormals[v] = (sum / count).normalized;
            }

            mesh.Normals = newNormals;
        }

        public static void HardenEdges(EditableMesh mesh, float angleThreshold)
        {
            if (mesh.Normals == null) RecalculateNormals(mesh);

            float cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);

            var newPositions = new List<Vector3>(mesh.Positions);
            var newNormals = new List<Vector3>(mesh.Normals);
            var newTris = (int[])mesh.Triangles.Clone();

            List<Vector2>[] newUVs = new List<Vector2>[8];
            for (int ch = 0; ch < 8; ch++)
                newUVs[ch] = mesh.UVChannels[ch] != null ? new List<Vector2>(mesh.UVChannels[ch]) : null;

            int triCount = mesh.TriangleCount;

            // For each triangle, compute its face normal
            var faceNormals = new Vector3[triCount];
            for (int t = 0; t < triCount; t++)
                faceNormals[t] = mesh.GetFaceNormal(t);

            // For each vertex, check if adjacent face normals differ too much
            // If so, split the vertex
            var vertToTris = new Dictionary<int, List<int>>();
            for (int t = 0; t < triCount; t++)
            {
                for (int e = 0; e < 3; e++)
                {
                    int v = mesh.Triangles[t * 3 + e];
                    if (!vertToTris.TryGetValue(v, out var list))
                    {
                        list = new List<int>();
                        vertToTris[v] = list;
                    }
                    list.Add(t);
                }
            }

            foreach (var kvp in vertToTris)
            {
                int vertIdx = kvp.Key;
                var tris = kvp.Value;
                if (tris.Count <= 1) continue;

                // Group triangles by similar normals
                var groups = new List<List<int>>();
                var assigned = new bool[tris.Count];

                for (int i = 0; i < tris.Count; i++)
                {
                    if (assigned[i]) continue;
                    var group = new List<int> { tris[i] };
                    assigned[i] = true;

                    for (int j = i + 1; j < tris.Count; j++)
                    {
                        if (assigned[j]) continue;
                        if (Vector3.Dot(faceNormals[tris[i]], faceNormals[tris[j]]) >= cosThreshold)
                        {
                            group.Add(tris[j]);
                            assigned[j] = true;
                        }
                    }
                    groups.Add(group);
                }

                if (groups.Count <= 1) continue;

                // First group keeps original vertex. Others get a new vertex.
                for (int g = 1; g < groups.Count; g++)
                {
                    int newIdx = newPositions.Count;
                    newPositions.Add(mesh.Positions[vertIdx]);

                    // Compute normal for this group
                    Vector3 groupNormal = Vector3.zero;
                    foreach (int t in groups[g])
                        groupNormal += faceNormals[t];
                    newNormals.Add(groupNormal.normalized);

                    for (int ch = 0; ch < 8; ch++)
                        newUVs[ch]?.Add(mesh.UVChannels[ch] != null && vertIdx < mesh.UVChannels[ch].Count
                            ? mesh.UVChannels[ch][vertIdx] : Vector2.zero);

                    // Remap triangles in this group
                    foreach (int t in groups[g])
                    {
                        for (int e = 0; e < 3; e++)
                        {
                            if (newTris[t * 3 + e] == vertIdx)
                                newTris[t * 3 + e] = newIdx;
                        }
                    }
                }

                // Update normal for first group
                Vector3 firstNormal = Vector3.zero;
                foreach (int t in groups[0])
                    firstNormal += faceNormals[t];
                if (vertIdx < newNormals.Count)
                    newNormals[vertIdx] = firstNormal.normalized;
            }

            mesh.Positions = newPositions.ToArray();
            mesh.Normals = newNormals.ToArray();
            mesh.Triangles = newTris;
            for (int ch = 0; ch < 8; ch++)
                mesh.UVChannels[ch] = newUVs[ch];
            mesh.InvalidateAdjacency();
        }

        public static void SetNormalDirection(EditableMesh mesh, HashSet<int> vertIndices, NormalDirection dir, Vector3 customTarget = default)
        {
            if (mesh.Normals == null) return;

            foreach (int v in vertIndices)
            {
                if (v >= mesh.Normals.Length) continue;

                mesh.Normals[v] = dir switch
                {
                    NormalDirection.Up => Vector3.up,
                    NormalDirection.Spherized => mesh.Positions[v].normalized,
                    NormalDirection.FaceAverage => ComputeVertexFaceAverage(mesh, v),
                    NormalDirection.TowardPoint => (customTarget - mesh.Positions[v]).normalized,
                    _ => mesh.Normals[v]
                };
            }
        }

        static Vector3 ComputeVertexFaceAverage(EditableMesh mesh, int vertIdx)
        {
            var tris = mesh.GetTrianglesForVertex(vertIdx);
            Vector3 sum = Vector3.zero;
            foreach (int t in tris)
                sum += mesh.GetFaceNormal(t);
            return sum.normalized;
        }

        public static void TransferNormals(EditableMesh source, EditableMesh target)
        {
            if (source.Normals == null) return;
            if (target.Normals == null)
                target.Normals = new Vector3[target.VertexCount];

            for (int v = 0; v < target.VertexCount; v++)
            {
                // Find closest vertex in source
                float bestDist = float.MaxValue;
                int bestIdx = 0;
                for (int s = 0; s < source.VertexCount; s++)
                {
                    float dist = (target.Positions[v] - source.Positions[s]).sqrMagnitude;
                    if (dist < bestDist) { bestDist = dist; bestIdx = s; }
                }
                target.Normals[v] = source.Normals[bestIdx];
            }
        }

        public enum NormalDirection { Up, Spherized, FaceAverage, TowardPoint }
    }
}
#endif

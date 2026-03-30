#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class MeshEditorOperations
    {
        #region Transform

        public static void MoveVertices(EditableMesh mesh, HashSet<int> vertIndices, Vector3 delta)
        {
            foreach (int v in vertIndices)
                mesh.Positions[v] += delta;
            mesh.InvalidateAdjacency();
        }

        public static void ScaleVertices(EditableMesh mesh, HashSet<int> vertIndices, Vector3 center, Vector3 scale)
        {
            foreach (int v in vertIndices)
            {
                Vector3 offset = mesh.Positions[v] - center;
                mesh.Positions[v] = center + Vector3.Scale(offset, scale);
            }
            mesh.InvalidateAdjacency();
        }

        public static void RotateVertices(EditableMesh mesh, HashSet<int> vertIndices, Vector3 center, Quaternion rotation)
        {
            foreach (int v in vertIndices)
            {
                Vector3 offset = mesh.Positions[v] - center;
                mesh.Positions[v] = center + rotation * offset;

                if (mesh.Normals != null && v < mesh.Normals.Length)
                    mesh.Normals[v] = rotation * mesh.Normals[v];
            }
            mesh.InvalidateAdjacency();
        }

        #endregion

        #region Extrude Faces

        public static HashSet<int> ExtrudeFaces(EditableMesh mesh, HashSet<int> faceIndices, float distance)
        {
            if (faceIndices.Count == 0) return new HashSet<int>();

            // Collect boundary edges of the selection
            var edgeCount = new Dictionary<long, int>();
            var selectedVerts = new HashSet<int>();

            foreach (int f in faceIndices)
            {
                for (int e = 0; e < 3; e++)
                {
                    int v0 = mesh.Triangles[f * 3 + e];
                    int v1 = mesh.Triangles[f * 3 + (e + 1) % 3];
                    selectedVerts.Add(v0);
                    selectedVerts.Add(v1);

                    long key = EditableMesh.EdgeKey(v0, v1);
                    edgeCount.TryGetValue(key, out int cnt);
                    edgeCount[key] = cnt + 1;
                }
            }

            // Boundary edges: shared by exactly one selected face
            var boundaryEdges = new List<(int v0, int v1)>();
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1)
                {
                    var (a, b) = EditableMesh.EdgeFromKey(kvp.Key);
                    boundaryEdges.Add((a, b));
                }
            }

            // Compute average normal of selected faces
            Vector3 extrudeDir = Vector3.zero;
            foreach (int f in faceIndices)
                extrudeDir += mesh.GetFaceNormal(f);
            extrudeDir = extrudeDir.normalized;

            // Duplicate vertices used by selected faces
            var oldToNew = new Dictionary<int, int>();
            int oldVertCount = mesh.Positions.Length;

            var newPositions = new List<Vector3>(mesh.Positions);
            var newNormals = mesh.Normals != null ? new List<Vector3>(mesh.Normals) : null;
            var newTangents = mesh.Tangents != null ? new List<Vector4>(mesh.Tangents) : null;
            var newColors = mesh.Colors != null && mesh.Colors.Length == oldVertCount
                ? new List<Color>(mesh.Colors) : null;

            List<Vector2>[] newUVs = new List<Vector2>[8];
            for (int ch = 0; ch < 8; ch++)
            {
                if (mesh.UVChannels[ch] != null && mesh.UVChannels[ch].Count == oldVertCount)
                    newUVs[ch] = new List<Vector2>(mesh.UVChannels[ch]);
            }

            foreach (int v in selectedVerts)
            {
                int newIdx = newPositions.Count;
                oldToNew[v] = newIdx;

                newPositions.Add(mesh.Positions[v] + extrudeDir * distance);
                newNormals?.Add(mesh.Normals[v]);
                newTangents?.Add(mesh.Tangents[v]);
                newColors?.Add(mesh.Colors[v]);
                for (int ch = 0; ch < 8; ch++)
                    newUVs[ch]?.Add(mesh.UVChannels[ch][v]);
            }

            // Remap selected faces to new vertices
            var newTris = new List<int>(mesh.Triangles);
            foreach (int f in faceIndices)
            {
                newTris[f * 3] = oldToNew[mesh.Triangles[f * 3]];
                newTris[f * 3 + 1] = oldToNew[mesh.Triangles[f * 3 + 1]];
                newTris[f * 3 + 2] = oldToNew[mesh.Triangles[f * 3 + 2]];
            }

            // Create side faces (connecting old boundary to new boundary)
            foreach (var (v0, v1) in boundaryEdges)
            {
                int nv0 = oldToNew[v0], nv1 = oldToNew[v1];
                // Two triangles forming a quad: v0-v1-nv1-nv0
                newTris.Add(v0); newTris.Add(v1); newTris.Add(nv1);
                newTris.Add(v0); newTris.Add(nv1); newTris.Add(nv0);
            }

            // Apply
            mesh.Positions = newPositions.ToArray();
            if (newNormals != null) mesh.Normals = newNormals.ToArray();
            if (newTangents != null) mesh.Tangents = newTangents.ToArray();
            if (newColors != null) mesh.Colors = newColors.ToArray();
            mesh.Triangles = newTris.ToArray();
            for (int ch = 0; ch < 8; ch++)
                if (newUVs[ch] != null) mesh.UVChannels[ch] = newUVs[ch];

            mesh.InvalidateAdjacency();

            // Return new vertex indices for the extruded selection
            return new HashSet<int>(oldToNew.Values);
        }

        #endregion

        #region Extrude Edges

        public static HashSet<int> ExtrudeEdges(EditableMesh mesh, HashSet<long> edgeKeys, float distance)
        {
            if (edgeKeys.Count == 0) return new HashSet<int>();

            // Collect all unique vertices from edges
            var edgeVerts = new HashSet<int>();
            foreach (long key in edgeKeys)
            {
                var (a, b) = EditableMesh.EdgeFromKey(key);
                edgeVerts.Add(a);
                edgeVerts.Add(b);
            }

            // Compute average normal of adjacent faces for extrude direction
            Vector3 extrudeDir = Vector3.zero;
            foreach (long key in edgeKeys)
            {
                var (a, b) = EditableMesh.EdgeFromKey(key);
                var tris = mesh.GetTrianglesForEdge(a, b);
                foreach (int t in tris)
                    extrudeDir += mesh.GetFaceNormal(t);
            }
            extrudeDir = extrudeDir.normalized;
            if (extrudeDir.sqrMagnitude < 0.01f) extrudeDir = Vector3.up;

            // Duplicate vertices
            var oldToNew = new Dictionary<int, int>();
            var newPositions = new List<Vector3>(mesh.Positions);
            var newNormals = mesh.Normals != null ? new List<Vector3>(mesh.Normals) : null;

            foreach (int v in edgeVerts)
            {
                int newIdx = newPositions.Count;
                oldToNew[v] = newIdx;
                newPositions.Add(mesh.Positions[v] + extrudeDir * distance);
                newNormals?.Add(mesh.Normals != null ? mesh.Normals[v] : Vector3.up);
            }

            // Create quad for each edge
            var newTris = new List<int>(mesh.Triangles);
            foreach (long key in edgeKeys)
            {
                var (v0, v1) = EditableMesh.EdgeFromKey(key);
                int nv0 = oldToNew[v0], nv1 = oldToNew[v1];
                newTris.Add(v0); newTris.Add(v1); newTris.Add(nv1);
                newTris.Add(v0); newTris.Add(nv1); newTris.Add(nv0);
            }

            mesh.Positions = newPositions.ToArray();
            if (newNormals != null) mesh.Normals = newNormals.ToArray();
            mesh.Triangles = newTris.ToArray();
            mesh.InvalidateAdjacency();

            return new HashSet<int>(oldToNew.Values);
        }

        #endregion

        #region Delete

        public static void DeleteFaces(EditableMesh mesh, HashSet<int> faceIndices)
        {
            mesh.DeleteTriangles(faceIndices);
        }

        public static void DeleteEdges(EditableMesh mesh, HashSet<long> edgeKeys)
        {
            // Delete all faces that contain any of the selected edges
            var facesToDelete = new HashSet<int>();
            foreach (long key in edgeKeys)
            {
                var (v0, v1) = EditableMesh.EdgeFromKey(key);
                facesToDelete.UnionWith(mesh.GetTrianglesForEdge(v0, v1));
            }
            mesh.DeleteTriangles(facesToDelete);
        }

        public static void DeleteVertices(EditableMesh mesh, HashSet<int> vertIndices)
        {
            mesh.DeleteVertices(vertIndices);
        }

        #endregion

        #region Merge / Weld

        public static int MergeVertices(EditableMesh mesh, HashSet<int> vertIndices)
        {
            if (vertIndices.Count < 2) return -1;

            // Compute centroid
            Vector3 center = Vector3.zero;
            foreach (int v in vertIndices)
                center += mesh.Positions[v];
            center /= vertIndices.Count;

            // Pick the first vertex as the "keep" vertex
            int keepIdx = vertIndices.First();
            mesh.Positions[keepIdx] = center;

            if (mesh.Normals != null && keepIdx < mesh.Normals.Length)
            {
                Vector3 avgNormal = Vector3.zero;
                foreach (int v in vertIndices)
                    if (v < mesh.Normals.Length) avgNormal += mesh.Normals[v];
                mesh.Normals[keepIdx] = avgNormal.normalized;
            }

            // Remap all triangle indices
            foreach (int v in vertIndices)
            {
                if (v == keepIdx) continue;
                for (int i = 0; i < mesh.Triangles.Length; i++)
                    if (mesh.Triangles[i] == v) mesh.Triangles[i] = keepIdx;
            }

            // Remove degenerate triangles
            mesh.RemoveDegenerateTriangles();
            mesh.RemoveUnusedVertices();

            return keepIdx;
        }

        #endregion

        #region Subdivide

        public static void SubdivideFaces(EditableMesh mesh, HashSet<int> faceIndices)
        {
            if (faceIndices.Count == 0) return;

            var newPositions = new List<Vector3>(mesh.Positions);
            var newNormals = mesh.Normals != null ? new List<Vector3>(mesh.Normals) : null;
            var newTris = new List<int>();
            int triCount = mesh.TriangleCount;

            // Midpoint cache
            var midCache = new Dictionary<long, int>();

            for (int t = 0; t < triCount; t++)
            {
                int i0 = mesh.Triangles[t * 3], i1 = mesh.Triangles[t * 3 + 1], i2 = mesh.Triangles[t * 3 + 2];

                if (!faceIndices.Contains(t))
                {
                    newTris.Add(i0); newTris.Add(i1); newTris.Add(i2);
                    continue;
                }

                // Get or create midpoints
                int m01 = GetOrCreateMidpoint(newPositions, newNormals, mesh, midCache, i0, i1);
                int m12 = GetOrCreateMidpoint(newPositions, newNormals, mesh, midCache, i1, i2);
                int m20 = GetOrCreateMidpoint(newPositions, newNormals, mesh, midCache, i2, i0);

                // 4 new triangles
                newTris.Add(i0); newTris.Add(m01); newTris.Add(m20);
                newTris.Add(m01); newTris.Add(i1); newTris.Add(m12);
                newTris.Add(m20); newTris.Add(m12); newTris.Add(i2);
                newTris.Add(m01); newTris.Add(m12); newTris.Add(m20);
            }

            mesh.Positions = newPositions.ToArray();
            if (newNormals != null) mesh.Normals = newNormals.ToArray();
            mesh.Triangles = newTris.ToArray();
            mesh.InvalidateAdjacency();
        }

        static int GetOrCreateMidpoint(List<Vector3> positions, List<Vector3> normals,
            EditableMesh mesh, Dictionary<long, int> cache, int a, int b)
        {
            long key = EditableMesh.EdgeKey(a, b);
            if (cache.TryGetValue(key, out int idx)) return idx;

            idx = positions.Count;
            positions.Add((mesh.Positions[a] + mesh.Positions[b]) * 0.5f);
            if (normals != null && mesh.Normals != null)
                normals.Add(((mesh.Normals[a] + mesh.Normals[b]) * 0.5f).normalized);

            cache[key] = idx;
            return idx;
        }

        #endregion

        #region Flip Normals

        public static void FlipNormals(EditableMesh mesh, HashSet<int> faceIndices)
        {
            if (faceIndices == null || faceIndices.Count == 0)
            {
                // Flip all
                if (mesh.Normals != null)
                    for (int i = 0; i < mesh.Normals.Length; i++)
                        mesh.Normals[i] = -mesh.Normals[i];

                // Reverse winding
                for (int i = 0; i < mesh.Triangles.Length; i += 3)
                    (mesh.Triangles[i + 1], mesh.Triangles[i + 2]) = (mesh.Triangles[i + 2], mesh.Triangles[i + 1]);
            }
            else
            {
                // Reverse winding for selected faces
                foreach (int f in faceIndices)
                {
                    int idx = f * 3;
                    (mesh.Triangles[idx + 1], mesh.Triangles[idx + 2]) = (mesh.Triangles[idx + 2], mesh.Triangles[idx + 1]);
                }

                // Flip normals of vertices in selected faces
                if (mesh.Normals != null)
                {
                    var verts = new HashSet<int>();
                    foreach (int f in faceIndices)
                    {
                        verts.Add(mesh.Triangles[f * 3]);
                        verts.Add(mesh.Triangles[f * 3 + 1]);
                        verts.Add(mesh.Triangles[f * 3 + 2]);
                    }
                    foreach (int v in verts)
                        if (v < mesh.Normals.Length) mesh.Normals[v] = -mesh.Normals[v];
                }
            }
            mesh.InvalidateAdjacency();
        }

        #endregion

        #region Fill Hole

        public static void FillHole(EditableMesh mesh, List<int> boundaryLoop)
        {
            if (boundaryLoop.Count < 3) return;

            // Simple fan triangulation from first vertex
            var newTris = new List<int>(mesh.Triangles);
            int pivot = boundaryLoop[0];
            for (int i = 1; i < boundaryLoop.Count - 1; i++)
            {
                newTris.Add(pivot);
                newTris.Add(boundaryLoop[i]);
                newTris.Add(boundaryLoop[i + 1]);
            }

            mesh.Triangles = newTris.ToArray();
            mesh.InvalidateAdjacency();
        }

        #endregion
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.Graphics3D.Modeling
{
    public class EditableMesh
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Color[] Colors;
        public BoneWeight[] BoneWeights;
        public int[] Triangles;
        public SubMeshDescriptor[] SubMeshes;

        // UV channels 0-7
        public List<Vector2>[] UVChannels = new List<Vector2>[8];

        // Lazy adjacency caches
        Dictionary<int, List<int>> _vertToTris;
        Dictionary<long, List<int>> _edgeToTris;
        bool _adjacencyDirty = true;

        #region Conversion

        public static EditableMesh FromMesh(Mesh mesh)
        {
            var em = new EditableMesh
            {
                Positions = mesh.vertices,
                Normals = mesh.normals,
                Tangents = mesh.tangents,
                Colors = mesh.colors,
                BoneWeights = mesh.boneWeights,
                Triangles = mesh.triangles
            };

            // Sub-meshes
            em.SubMeshes = new SubMeshDescriptor[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
                em.SubMeshes[i] = mesh.GetSubMesh(i);

            // UVs
            for (int ch = 0; ch < 8; ch++)
            {
                var uvs = new List<Vector2>();
                mesh.GetUVs(ch, uvs);
                em.UVChannels[ch] = uvs.Count > 0 ? uvs : null;
            }

            return em;
        }

        public Mesh ToMesh(Mesh target = null)
        {
            if (target == null)
                target = new Mesh();
            else
                target.Clear();

            if (Positions.Length > 65535)
                target.indexFormat = IndexFormat.UInt32;

            target.vertices = Positions;

            if (Normals != null && Normals.Length == Positions.Length)
                target.normals = Normals;
            if (Tangents != null && Tangents.Length == Positions.Length)
                target.tangents = Tangents;
            if (Colors != null && Colors.Length == Positions.Length)
                target.colors = Colors;
            if (BoneWeights != null && BoneWeights.Length == Positions.Length)
                target.boneWeights = BoneWeights;

            target.triangles = Triangles;

            for (int ch = 0; ch < 8; ch++)
            {
                if (UVChannels[ch] != null && UVChannels[ch].Count == Positions.Length)
                    target.SetUVs(ch, UVChannels[ch]);
            }

            if (SubMeshes != null && SubMeshes.Length > 1)
            {
                target.subMeshCount = SubMeshes.Length;
                for (int i = 0; i < SubMeshes.Length; i++)
                    target.SetSubMesh(i, SubMeshes[i]);
            }

            if (Normals == null || Normals.Length != Positions.Length)
                target.RecalculateNormals();

            target.RecalculateBounds();
            return target;
        }

        #endregion

        #region Adjacency

        public void InvalidateAdjacency()
        {
            _adjacencyDirty = true;
            _vertToTris = null;
            _edgeToTris = null;
            // Reset submeshes to cover all triangles (old descriptors are stale after topology changes)
            if (Triangles != null)
                SubMeshes = new[] { new SubMeshDescriptor(0, Triangles.Length) };
        }

        void EnsureAdjacency()
        {
            if (!_adjacencyDirty) return;
            _adjacencyDirty = false;

            _vertToTris = new Dictionary<int, List<int>>();
            _edgeToTris = new Dictionary<long, List<int>>();

            int triCount = Triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = Triangles[t * 3], i1 = Triangles[t * 3 + 1], i2 = Triangles[t * 3 + 2];

                AddVertTri(i0, t);
                AddVertTri(i1, t);
                AddVertTri(i2, t);

                AddEdgeTri(i0, i1, t);
                AddEdgeTri(i1, i2, t);
                AddEdgeTri(i2, i0, t);
            }
        }

        void AddVertTri(int vert, int tri)
        {
            if (!_vertToTris.TryGetValue(vert, out var list))
            {
                list = new List<int>(6);
                _vertToTris[vert] = list;
            }
            list.Add(tri);
        }

        void AddEdgeTri(int v0, int v1, int tri)
        {
            long key = EdgeKey(v0, v1);
            if (!_edgeToTris.TryGetValue(key, out var list))
            {
                list = new List<int>(2);
                _edgeToTris[key] = list;
            }
            list.Add(tri);
        }

        public static long EdgeKey(int v0, int v1)
        {
            int lo = v0 < v1 ? v0 : v1;
            int hi = v0 < v1 ? v1 : v0;
            return ((long)lo << 32) | (uint)hi;
        }

        public static (int, int) EdgeFromKey(long key)
        {
            return ((int)(key >> 32), (int)(key & 0xFFFFFFFF));
        }

        #endregion

        #region Queries

        public List<int> GetTrianglesForVertex(int vertIdx)
        {
            EnsureAdjacency();
            return _vertToTris.TryGetValue(vertIdx, out var list) ? list : new List<int>();
        }

        public List<int> GetTrianglesForEdge(int v0, int v1)
        {
            EnsureAdjacency();
            return _edgeToTris.TryGetValue(EdgeKey(v0, v1), out var list) ? list : new List<int>();
        }

        public HashSet<int> GetConnectedVertices(int vertIdx)
        {
            EnsureAdjacency();
            var connected = new HashSet<int>();
            if (!_vertToTris.TryGetValue(vertIdx, out var tris)) return connected;

            foreach (int t in tris)
            {
                connected.Add(Triangles[t * 3]);
                connected.Add(Triangles[t * 3 + 1]);
                connected.Add(Triangles[t * 3 + 2]);
            }
            connected.Remove(vertIdx);
            return connected;
        }

        public Vector3 GetFaceNormal(int triIndex)
        {
            int i0 = Triangles[triIndex * 3];
            int i1 = Triangles[triIndex * 3 + 1];
            int i2 = Triangles[triIndex * 3 + 2];
            return MeshGeometryUtils.ComputeFaceNormal(Positions[i0], Positions[i1], Positions[i2]);
        }

        public Vector3 GetFaceCenter(int triIndex)
        {
            int i0 = Triangles[triIndex * 3];
            int i1 = Triangles[triIndex * 3 + 1];
            int i2 = Triangles[triIndex * 3 + 2];
            return (Positions[i0] + Positions[i1] + Positions[i2]) / 3f;
        }

        public HashSet<long> GetAllEdges()
        {
            var edges = new HashSet<long>();
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                edges.Add(EdgeKey(Triangles[i], Triangles[i + 1]));
                edges.Add(EdgeKey(Triangles[i + 1], Triangles[i + 2]));
                edges.Add(EdgeKey(Triangles[i + 2], Triangles[i]));
            }
            return edges;
        }

        public bool IsBoundaryEdge(int v0, int v1)
        {
            var tris = GetTrianglesForEdge(v0, v1);
            return tris.Count == 1;
        }

        public bool IsBoundaryVertex(int vertIdx)
        {
            var connected = GetConnectedVertices(vertIdx);
            foreach (int other in connected)
            {
                if (IsBoundaryEdge(vertIdx, other))
                    return true;
            }
            return false;
        }

        public List<long> GetEdgeLoop(int v0, int v1)
        {
            EnsureAdjacency();
            var loop = new List<long> { EdgeKey(v0, v1) };
            var visited = new HashSet<long> { EdgeKey(v0, v1) };

            // Walk forward
            WalkEdgeLoop(v0, v1, loop, visited, false);
            // Walk backward
            WalkEdgeLoop(v1, v0, loop, visited, true);

            return loop;
        }

        void WalkEdgeLoop(int fromVert, int toVert, List<long> loop, HashSet<long> visited, bool prepend)
        {
            int current = toVert;
            int prev = fromVert;
            int maxIterations = Mathf.Max(VertexCount, 10000);

            int safety;
            for (safety = 0; safety < maxIterations; safety++)
            {
                // Find the triangle on the "other side" of the current edge
                var tris = GetTrianglesForEdge(prev, current);
                if (tris.Count != 2) break; // boundary or non-manifold

                // For each adjacent tri, find the opposite edge (the edge not containing prev)
                int nextVert = -1;
                foreach (int t in tris)
                {
                    int i0 = Triangles[t * 3], i1 = Triangles[t * 3 + 1], i2 = Triangles[t * 3 + 2];
                    // Find the vertex in this tri that isn't prev or current
                    int opposite = -1;
                    if (i0 != prev && i0 != current) opposite = i0;
                    else if (i1 != prev && i1 != current) opposite = i1;
                    else if (i2 != prev && i2 != current) opposite = i2;

                    if (opposite < 0) continue;

                    // The next edge in the loop is the one from current through the quad
                    // For quads (two tris sharing an edge), find the edge opposite to prev-current
                    var oppositeTris = GetTrianglesForEdge(current, opposite);
                    if (oppositeTris.Count == 2)
                    {
                        long edgeKey = EdgeKey(current, opposite);
                        if (!visited.Contains(edgeKey))
                        {
                            nextVert = opposite;
                            break;
                        }
                    }
                }

                if (nextVert < 0) break;

                long key = EdgeKey(current, nextVert);
                if (visited.Contains(key)) break;
                visited.Add(key);

                if (prepend)
                    loop.Insert(0, key);
                else
                    loop.Add(key);

                prev = current;
                current = nextVert;
            }

            if (safety >= maxIterations)
                Debug.LogWarning($"EditableMesh.WalkEdgeLoop: Safety limit ({maxIterations}) reached. Edge loop may be incomplete.");
        }

        #endregion

        #region Topology Operations

        public void WeldVertices(float threshold)
        {
            float thresholdSq = threshold * threshold;
            int vertCount = Positions.Length;
            int[] remap = new int[vertCount];
            var kept = new List<int>();

            for (int i = 0; i < vertCount; i++)
                remap[i] = -1;

            for (int i = 0; i < vertCount; i++)
            {
                if (remap[i] >= 0) continue;

                remap[i] = kept.Count;
                kept.Add(i);

                for (int j = i + 1; j < vertCount; j++)
                {
                    if (remap[j] >= 0) continue;
                    if ((Positions[i] - Positions[j]).sqrMagnitude <= thresholdSq)
                        remap[j] = remap[i];
                }
            }

            // Rebuild arrays
            int newCount = kept.Count;
            var newPositions = new Vector3[newCount];
            var newNormals = Normals != null ? new Vector3[newCount] : null;
            var newTangents = Tangents != null ? new Vector4[newCount] : null;
            var newColors = Colors != null && Colors.Length == vertCount ? new Color[newCount] : null;

            for (int i = 0; i < newCount; i++)
            {
                int src = kept[i];
                newPositions[i] = Positions[src];
                if (newNormals != null) newNormals[i] = Normals[src];
                if (newTangents != null) newTangents[i] = Tangents[src];
                if (newColors != null) newColors[i] = Colors[src];
            }

            // Remap triangles and remove degenerates
            var newTris = new List<int>(Triangles.Length);
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                int a = remap[Triangles[i]], b = remap[Triangles[i + 1]], c = remap[Triangles[i + 2]];
                if (a == b || b == c || c == a) continue; // degenerate
                newTris.Add(a);
                newTris.Add(b);
                newTris.Add(c);
            }

            Positions = newPositions;
            Normals = newNormals;
            Tangents = newTangents;
            Colors = newColors;
            Triangles = newTris.ToArray();

            // Remap UVs
            for (int ch = 0; ch < 8; ch++)
            {
                if (UVChannels[ch] == null || UVChannels[ch].Count != vertCount) continue;
                var newUVs = new List<Vector2>(newCount);
                for (int i = 0; i < newCount; i++)
                    newUVs.Add(UVChannels[ch][kept[i]]);
                UVChannels[ch] = newUVs;
            }

            InvalidateAdjacency();
        }

        public void DeleteTriangles(HashSet<int> triIndices)
        {
            var newTris = new List<int>();
            int triCount = Triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                if (triIndices.Contains(t)) continue;
                newTris.Add(Triangles[t * 3]);
                newTris.Add(Triangles[t * 3 + 1]);
                newTris.Add(Triangles[t * 3 + 2]);
            }
            Triangles = newTris.ToArray();
            InvalidateAdjacency();
        }

        public void DeleteVertices(HashSet<int> vertIndices)
        {
            // Remove any triangle referencing a deleted vertex
            var trisToRemove = new HashSet<int>();
            int triCount = Triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                if (vertIndices.Contains(Triangles[t * 3]) ||
                    vertIndices.Contains(Triangles[t * 3 + 1]) ||
                    vertIndices.Contains(Triangles[t * 3 + 2]))
                    trisToRemove.Add(t);
            }
            DeleteTriangles(trisToRemove);
            RemoveUnusedVertices();
        }

        public void RemoveUnusedVertices()
        {
            int vertCount = Positions.Length;
            var used = new bool[vertCount];
            foreach (int idx in Triangles)
                if (idx < vertCount) used[idx] = true;

            int[] remap = new int[vertCount];
            var kept = new List<int>();
            for (int i = 0; i < vertCount; i++)
            {
                if (used[i])
                {
                    remap[i] = kept.Count;
                    kept.Add(i);
                }
                else
                {
                    remap[i] = -1;
                }
            }

            if (kept.Count == vertCount) return; // nothing to remove

            CompactVertices(kept, remap);
        }

        public void RemoveDegenerateTriangles()
        {
            var newTris = new List<int>();
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                int a = Triangles[i], b = Triangles[i + 1], c = Triangles[i + 2];
                if (a == b || b == c || c == a) continue;

                float area = MeshGeometryUtils.TriangleArea(Positions[a], Positions[b], Positions[c]);
                if (area < 1e-8f) continue;

                newTris.Add(a);
                newTris.Add(b);
                newTris.Add(c);
            }
            Triangles = newTris.ToArray();
            InvalidateAdjacency();
        }

        void CompactVertices(List<int> kept, int[] remap)
        {
            int newCount = kept.Count;
            var newPos = new Vector3[newCount];
            var newNorm = Normals != null ? new Vector3[newCount] : null;
            var newTang = Tangents != null ? new Vector4[newCount] : null;
            var newCol = Colors != null && Colors.Length == Positions.Length ? new Color[newCount] : null;

            for (int i = 0; i < newCount; i++)
            {
                int src = kept[i];
                newPos[i] = Positions[src];
                if (newNorm != null && src < Normals.Length) newNorm[i] = Normals[src];
                if (newTang != null && src < Tangents.Length) newTang[i] = Tangents[src];
                if (newCol != null && src < Colors.Length) newCol[i] = Colors[src];
            }

            for (int i = 0; i < Triangles.Length; i++)
                Triangles[i] = remap[Triangles[i]];

            for (int ch = 0; ch < 8; ch++)
            {
                if (UVChannels[ch] == null || UVChannels[ch].Count != Positions.Length) continue;
                var newUVs = new List<Vector2>(newCount);
                for (int i = 0; i < newCount; i++)
                    newUVs.Add(UVChannels[ch][kept[i]]);
                UVChannels[ch] = newUVs;
            }

            Positions = newPos;
            Normals = newNorm;
            Tangents = newTang;
            Colors = newCol;
            InvalidateAdjacency();
        }

        #endregion

        #region Properties

        public int VertexCount => Positions?.Length ?? 0;
        public int TriangleCount => (Triangles?.Length ?? 0) / 3;

        public int GetNonManifoldEdgeCount()
        {
            EnsureAdjacency();
            int count = 0;
            foreach (var kvp in _edgeToTris)
                if (kvp.Value.Count > 2) count++;
            return count;
        }

        public int GetBoundaryEdgeCount()
        {
            EnsureAdjacency();
            int count = 0;
            foreach (var kvp in _edgeToTris)
                if (kvp.Value.Count == 1) count++;
            return count;
        }

        public int GetUnusedVertexCount()
        {
            var used = new bool[Positions.Length];
            foreach (int idx in Triangles)
                if (idx < Positions.Length) used[idx] = true;

            int count = 0;
            for (int i = 0; i < used.Length; i++)
                if (!used[i]) count++;
            return count;
        }

        public int GetDegenerateTriangleCount()
        {
            int count = 0;
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                int a = Triangles[i], b = Triangles[i + 1], c = Triangles[i + 2];
                if (a == b || b == c || c == a) { count++; continue; }
                float area = MeshGeometryUtils.TriangleArea(Positions[a], Positions[b], Positions[c]);
                if (area < 1e-8f) count++;
            }
            return count;
        }

        public int GetDuplicateVertexCount(float threshold = 0.0001f)
        {
            float thresholdSq = threshold * threshold;
            int count = 0;
            // Simple O(n^2) check — fine for inspection, not for large meshes at runtime
            for (int i = 0; i < Positions.Length; i++)
            for (int j = i + 1; j < Positions.Length; j++)
            {
                if ((Positions[i] - Positions[j]).sqrMagnitude <= thresholdSq)
                {
                    count++;
                    break; // count each vertex at most once
                }
            }
            return count;
        }

        #endregion
    }
}
#endif

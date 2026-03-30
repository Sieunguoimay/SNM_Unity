#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public enum SelectionMode { Vertex, Edge, Face }

    public class MeshSelection
    {
        public SelectionMode Mode = SelectionMode.Vertex;
        public readonly HashSet<int> Vertices = new();
        public readonly HashSet<long> Edges = new();
        public readonly HashSet<int> Faces = new();

        // Static cache keyed by mesh instance ID
        static readonly Dictionary<int, MeshSelection> _cache = new();

        public static MeshSelection GetOrCreate(Mesh mesh)
        {
            int id = mesh.GetInstanceID();
            if (!_cache.TryGetValue(id, out var sel))
            {
                sel = new MeshSelection();
                _cache[id] = sel;
            }
            return sel;
        }

        public static void ClearCache() => _cache.Clear();

        public bool HasSelection => Vertices.Count > 0 || Edges.Count > 0 || Faces.Count > 0;

        public void Clear()
        {
            Vertices.Clear();
            Edges.Clear();
            Faces.Clear();
        }

        #region Select Operations

        public void SelectAll(EditableMesh mesh)
        {
            Clear();
            switch (Mode)
            {
                case SelectionMode.Vertex:
                    for (int i = 0; i < mesh.VertexCount; i++) Vertices.Add(i);
                    break;
                case SelectionMode.Edge:
                    Edges.UnionWith(mesh.GetAllEdges());
                    break;
                case SelectionMode.Face:
                    for (int i = 0; i < mesh.TriangleCount; i++) Faces.Add(i);
                    break;
            }
        }

        public void SelectNone() => Clear();

        public void InvertSelection(EditableMesh mesh)
        {
            switch (Mode)
            {
                case SelectionMode.Vertex:
                    var allVerts = new HashSet<int>();
                    for (int i = 0; i < mesh.VertexCount; i++) allVerts.Add(i);
                    allVerts.ExceptWith(Vertices);
                    Vertices.Clear();
                    Vertices.UnionWith(allVerts);
                    break;
                case SelectionMode.Edge:
                    var allEdges = mesh.GetAllEdges();
                    allEdges.ExceptWith(Edges);
                    Edges.Clear();
                    Edges.UnionWith(allEdges);
                    break;
                case SelectionMode.Face:
                    var allFaces = new HashSet<int>();
                    for (int i = 0; i < mesh.TriangleCount; i++) allFaces.Add(i);
                    allFaces.ExceptWith(Faces);
                    Faces.Clear();
                    Faces.UnionWith(allFaces);
                    break;
            }
        }

        public void GrowSelection(EditableMesh mesh)
        {
            switch (Mode)
            {
                case SelectionMode.Vertex:
                    var newVerts = new HashSet<int>();
                    foreach (int v in Vertices)
                        newVerts.UnionWith(mesh.GetConnectedVertices(v));
                    Vertices.UnionWith(newVerts);
                    break;
                case SelectionMode.Face:
                    var newFaces = new HashSet<int>();
                    foreach (int f in Faces)
                    {
                        int i0 = mesh.Triangles[f * 3];
                        int i1 = mesh.Triangles[f * 3 + 1];
                        int i2 = mesh.Triangles[f * 3 + 2];
                        newFaces.UnionWith(mesh.GetTrianglesForVertex(i0));
                        newFaces.UnionWith(mesh.GetTrianglesForVertex(i1));
                        newFaces.UnionWith(mesh.GetTrianglesForVertex(i2));
                    }
                    Faces.UnionWith(newFaces);
                    break;
            }
        }

        public void ShrinkSelection(EditableMesh mesh)
        {
            switch (Mode)
            {
                case SelectionMode.Vertex:
                    var boundary = new HashSet<int>();
                    foreach (int v in Vertices)
                    {
                        var connected = mesh.GetConnectedVertices(v);
                        foreach (int c in connected)
                        {
                            if (!Vertices.Contains(c))
                            {
                                boundary.Add(v);
                                break;
                            }
                        }
                    }
                    Vertices.ExceptWith(boundary);
                    break;
                case SelectionMode.Face:
                    var boundaryFaces = new HashSet<int>();
                    foreach (int f in Faces)
                    {
                        for (int e = 0; e < 3; e++)
                        {
                            int v0 = mesh.Triangles[f * 3 + e];
                            int v1 = mesh.Triangles[f * 3 + (e + 1) % 3];
                            var adjTris = mesh.GetTrianglesForEdge(v0, v1);
                            bool allSelected = true;
                            foreach (int adj in adjTris)
                                if (!Faces.Contains(adj)) { allSelected = false; break; }
                            if (!allSelected) { boundaryFaces.Add(f); break; }
                        }
                    }
                    Faces.ExceptWith(boundaryFaces);
                    break;
            }
        }

        public void SelectLinked(EditableMesh mesh)
        {
            if (Mode == SelectionMode.Vertex && Vertices.Count > 0)
            {
                var queue = new Queue<int>(Vertices);
                while (queue.Count > 0)
                {
                    int v = queue.Dequeue();
                    foreach (int c in mesh.GetConnectedVertices(v))
                    {
                        if (Vertices.Add(c))
                            queue.Enqueue(c);
                    }
                }
            }
            else if (Mode == SelectionMode.Face && Faces.Count > 0)
            {
                // Convert to vertices, flood fill, convert back
                var verts = GetSelectedVertices(mesh);
                var queue = new Queue<int>(verts);
                var visited = new HashSet<int>(verts);
                while (queue.Count > 0)
                {
                    int v = queue.Dequeue();
                    foreach (int c in mesh.GetConnectedVertices(v))
                    {
                        if (visited.Add(c))
                            queue.Enqueue(c);
                    }
                }
                // Add all faces whose vertices are all in visited set
                for (int f = 0; f < mesh.TriangleCount; f++)
                {
                    int i0 = mesh.Triangles[f * 3], i1 = mesh.Triangles[f * 3 + 1], i2 = mesh.Triangles[f * 3 + 2];
                    if (visited.Contains(i0) && visited.Contains(i1) && visited.Contains(i2))
                        Faces.Add(f);
                }
            }
        }

        #endregion

        #region Conversion

        public HashSet<int> GetSelectedVertices(EditableMesh mesh)
        {
            var result = new HashSet<int>(Vertices);

            foreach (long edgeKey in Edges)
            {
                var (a, b) = EditableMesh.EdgeFromKey(edgeKey);
                result.Add(a);
                result.Add(b);
            }

            foreach (int f in Faces)
            {
                result.Add(mesh.Triangles[f * 3]);
                result.Add(mesh.Triangles[f * 3 + 1]);
                result.Add(mesh.Triangles[f * 3 + 2]);
            }

            return result;
        }

        public Vector3 GetSelectionCenter(EditableMesh mesh)
        {
            var verts = GetSelectedVertices(mesh);
            if (verts.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (int v in verts)
                sum += mesh.Positions[v];
            return sum / verts.Count;
        }

        public Bounds GetSelectionBounds(EditableMesh mesh)
        {
            var verts = GetSelectedVertices(mesh);
            if (verts.Count == 0) return new Bounds();

            var enumerator = verts.GetEnumerator();
            enumerator.MoveNext();
            Vector3 min = mesh.Positions[enumerator.Current];
            Vector3 max = min;

            while (enumerator.MoveNext())
            {
                Vector3 p = mesh.Positions[enumerator.Current];
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        #endregion
    }
}
#endif

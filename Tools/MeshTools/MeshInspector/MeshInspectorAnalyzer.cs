#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.Tools.MeshTools
{
    public static class MeshInspectorAnalyzer
    {
        public struct MeshStats
        {
            public int VertexCount;
            public int TriangleCount;
            public int SubMeshCount;
            public IndexFormat IndexFormat;
            public Bounds Bounds;
            public bool IsReadable;

            // Attributes
            public bool HasNormals;
            public bool HasTangents;
            public bool HasColors;
            public bool HasBoneWeights;
            public bool[] HasUV; // 0-7

            // Memory
            public long EstimatedMemoryBytes;

            // Per-submesh
            public int[] SubMeshTriCounts;

            // Topology issues
            public int DegenerateTriangles;
            public int UnusedVertices;
            public int DuplicateVertices;
            public int NonManifoldEdges;
            public int BoundaryEdges;
            public int EdgeCount;
        }

        public static MeshStats Analyze(Mesh mesh)
        {
            var stats = new MeshStats
            {
                VertexCount = mesh.vertexCount,
                TriangleCount = mesh.triangles.Length / 3,
                SubMeshCount = mesh.subMeshCount,
                IndexFormat = mesh.indexFormat,
                Bounds = mesh.bounds,
                IsReadable = mesh.isReadable,
                HasNormals = mesh.normals != null && mesh.normals.Length > 0,
                HasTangents = mesh.tangents != null && mesh.tangents.Length > 0,
                HasColors = mesh.colors != null && mesh.colors.Length > 0,
                HasBoneWeights = mesh.boneWeights != null && mesh.boneWeights.Length > 0,
                HasUV = new bool[8]
            };

            // UV channels
            for (int ch = 0; ch < 8; ch++)
            {
                var uvs = new List<Vector2>();
                mesh.GetUVs(ch, uvs);
                stats.HasUV[ch] = uvs.Count > 0;
            }

            // Per-submesh tri counts
            stats.SubMeshTriCounts = new int[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
                stats.SubMeshTriCounts[i] = mesh.GetSubMesh(i).indexCount / 3;

            // Memory estimate
            stats.EstimatedMemoryBytes = EstimateMemory(stats);

            // Topology analysis (only if readable)
            if (mesh.isReadable)
            {
                var em = EditableMesh.FromMesh(mesh);
                stats.DegenerateTriangles = em.GetDegenerateTriangleCount();
                stats.UnusedVertices = em.GetUnusedVertexCount();
                stats.NonManifoldEdges = em.GetNonManifoldEdgeCount();
                stats.BoundaryEdges = em.GetBoundaryEdgeCount();
                stats.EdgeCount = em.GetAllEdges().Count;

                // Duplicate vertices (can be slow for large meshes, cap it)
                if (mesh.vertexCount <= 10000)
                    stats.DuplicateVertices = em.GetDuplicateVertexCount();
                else
                    stats.DuplicateVertices = -1; // skip
            }

            return stats;
        }

        static long EstimateMemory(MeshStats stats)
        {
            long bytes = 0;
            int v = stats.VertexCount;

            // Positions: 12 bytes per vertex
            bytes += v * 12L;

            if (stats.HasNormals) bytes += v * 12L;
            if (stats.HasTangents) bytes += v * 16L;
            if (stats.HasColors) bytes += v * 16L; // Color = 4 floats
            if (stats.HasBoneWeights) bytes += v * 32L; // BoneWeight = 4 weights + 4 indices

            for (int ch = 0; ch < 8; ch++)
                if (stats.HasUV[ch]) bytes += v * 8L; // Vector2

            // Index buffer
            int indexSize = stats.IndexFormat == IndexFormat.UInt32 ? 4 : 2;
            bytes += stats.TriangleCount * 3L * indexSize;

            return bytes;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
#endif

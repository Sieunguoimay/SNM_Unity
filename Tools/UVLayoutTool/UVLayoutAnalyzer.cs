#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Tools.UVLayoutTool
{
    public static class UVLayoutAnalyzer
    {
        public struct UVStats
        {
            public float CoveragePercent;
            public int IslandCount;
            public int TriangleCount;
            public bool HasUVs;
        }

        public struct LightmapValidation
        {
            public bool HasUV1;
            public int OverlappingTriCount;
            public int OutOfBoundsTriCount;
            public float MinPaddingPixels;
            public float CoveragePercent;
            public List<string> Issues;
        }

        #region Core

        public static Vector2[] GetUVChannel(Mesh mesh, int channel)
        {
            var uvs = new List<Vector2>();
            mesh.GetUVs(channel, uvs);
            return uvs.ToArray();
        }

        public static UVStats Analyze(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;

            if (uvs.Length == 0)
                return new UVStats { HasUVs = false };

            int triCount = triangles.Length / 3;
            float totalArea = 0f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                Vector2 a = uvs[i0], b = uvs[i1], c = uvs[i2];
                totalArea += Mathf.Abs(Cross2D(b - a, c - a)) * 0.5f;
            }

            int islandCount = GetIslands(mesh, uvChannel).Count;

            return new UVStats
            {
                CoveragePercent = totalArea * 100f,
                IslandCount = islandCount,
                TriangleCount = triCount,
                HasUVs = true
            };
        }

        #endregion

        #region Islands

        public static List<List<int>> GetIslands(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;
            int vertCount = uvs.Length;

            if (vertCount == 0) return new List<List<int>>();

            int[] parent = new int[vertCount];
            int[] rank = new int[vertCount];
            for (int i = 0; i < vertCount; i++) parent[i] = i;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= vertCount || i1 >= vertCount || i2 >= vertCount) continue;
                Union(parent, rank, i0, i1);
                Union(parent, rank, i1, i2);
            }

            var rootToIsland = new Dictionary<int, int>();
            var islands = new List<List<int>>();

            int triCount = triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3];
                if (i0 >= vertCount) continue;

                int root = Find(parent, i0);
                if (!rootToIsland.TryGetValue(root, out int islandIdx))
                {
                    islandIdx = islands.Count;
                    rootToIsland[root] = islandIdx;
                    islands.Add(new List<int>());
                }
                islands[islandIdx].Add(t);
            }

            return islands;
        }

        public static Color[] GenerateIslandColors(int count)
        {
            var colors = new Color[count];
            float goldenRatio = 0.618033988749895f;
            float hue = 0f;
            for (int i = 0; i < count; i++)
            {
                colors[i] = Color.HSVToRGB(hue, 0.75f, 0.95f);
                hue = (hue + goldenRatio) % 1f;
            }
            return colors;
        }

        #endregion

        #region Seams

        public static List<(Vector2 a, Vector2 b)> FindSeamEdges(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;
            var seams = new List<(Vector2, Vector2)>();

            if (uvs.Length == 0) return seams;

            // Build edge map: sorted vertex index pair -> list of (triIndex, uv0, uv1)
            var edgeMap = new Dictionary<long, List<(int tri, Vector2 uvA, Vector2 uvB)>>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int[] verts = { triangles[t], triangles[t + 1], triangles[t + 2] };
                for (int e = 0; e < 3; e++)
                {
                    int v0 = verts[e], v1 = verts[(e + 1) % 3];
                    if (v0 >= uvs.Length || v1 >= uvs.Length) continue;

                    int lo = Mathf.Min(v0, v1), hi = Mathf.Max(v0, v1);
                    long key = (long)lo << 32 | (uint)hi;

                    if (!edgeMap.TryGetValue(key, out var list))
                    {
                        list = new List<(int, Vector2, Vector2)>();
                        edgeMap[key] = list;
                    }
                    list.Add((t / 3, uvs[v0], uvs[v1]));
                }
            }

            // A seam is an edge shared by 2+ triangles where UVs differ
            foreach (var kvp in edgeMap)
            {
                var list = kvp.Value;
                if (list.Count < 2) continue;

                var first = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    var other = list[i];
                    // Compare UV positions (order may be swapped)
                    bool match1 = ApproxEqual(first.uvA, other.uvA) && ApproxEqual(first.uvB, other.uvB);
                    bool match2 = ApproxEqual(first.uvA, other.uvB) && ApproxEqual(first.uvB, other.uvA);
                    if (!match1 && !match2)
                    {
                        // This edge is a seam — add both UV representations
                        seams.Add((first.uvA, first.uvB));
                        break;
                    }
                }
            }

            return seams;
        }

        static bool ApproxEqual(Vector2 a, Vector2 b) =>
            Mathf.Abs(a.x - b.x) < 1e-5f && Mathf.Abs(a.y - b.y) < 1e-5f;

        #endregion

        #region Out of Bounds

        public static HashSet<int> GetOutOfBoundsTriangles(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;
            var result = new HashSet<int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                if (IsOutOfBounds(uvs[i0]) || IsOutOfBounds(uvs[i1]) || IsOutOfBounds(uvs[i2]))
                    result.Add(t / 3);
            }
            return result;
        }

        static bool IsOutOfBounds(Vector2 uv) =>
            uv.x < -0.001f || uv.x > 1.001f || uv.y < -0.001f || uv.y > 1.001f;

        #endregion

        #region Texel Density

        public static float[] ComputeTexelDensity(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            int triCount = triangles.Length / 3;
            var densities = new float[triCount];

            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                if (i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length) continue;

                // UV area
                Vector2 uvA = uvs[i0], uvB = uvs[i1], uvC = uvs[i2];
                float uvArea = Mathf.Abs(Cross2D(uvB - uvA, uvC - uvA)) * 0.5f;

                // World area
                Vector3 wA = vertices[i0], wB = vertices[i1], wC = vertices[i2];
                float worldArea = Vector3.Cross(wB - wA, wC - wA).magnitude * 0.5f;

                if (worldArea > 1e-8f)
                    densities[t] = uvArea / worldArea;
            }

            return densities;
        }

        public static Color DensityToColor(float density, float minDensity, float maxDensity)
        {
            if (maxDensity <= minDensity) return Color.gray;
            float t = Mathf.InverseLerp(minDensity, maxDensity, density);
            // Blue (low) -> Green (mid) -> Red (high)
            if (t < 0.5f)
                return Color.Lerp(new Color(0, 0, 1), new Color(0, 1, 0), t * 2f);
            return Color.Lerp(new Color(0, 1, 0), new Color(1, 0, 0), (t - 0.5f) * 2f);
        }

        #endregion

        #region Vertex Density

        public static float[,] ComputeVertexDensityMap(Vector2[] uvs, int resolution, float radius)
        {
            var map = new float[resolution, resolution];
            int radiusPx = Mathf.Max(1, Mathf.RoundToInt(radius * resolution));
            float invRadius = 1f / Mathf.Max(1, radiusPx);

            for (int v = 0; v < uvs.Length; v++)
            {
                int cx = Mathf.RoundToInt(uvs[v].x * (resolution - 1));
                int cy = Mathf.RoundToInt(uvs[v].y * (resolution - 1));

                for (int dy = -radiusPx; dy <= radiusPx; dy++)
                for (int dx = -radiusPx; dx <= radiusPx; dx++)
                {
                    int px = cx + dx, py = cy + dy;
                    if (px < 0 || px >= resolution || py < 0 || py >= resolution) continue;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * invRadius;
                    if (dist <= 1f)
                        map[px, py] += 1f - dist; // linear falloff
                }
            }

            return map;
        }

        public static float FindMaxDensity(float[,] map, int resolution)
        {
            float max = 0f;
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                if (map[x, y] > max) max = map[x, y];
            return max;
        }

        #endregion

        #region Island Padding

        public static float ComputeMinIslandPadding(Mesh mesh, int uvChannel, int textureResolution)
        {
            var islands = GetIslands(mesh, uvChannel);
            if (islands.Count < 2) return float.MaxValue;

            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;

            // Collect boundary edges per island
            var islandBoundaries = new List<List<Vector2>>();
            foreach (var island in islands)
            {
                var edgeCount = new Dictionary<long, int>();
                var edgeUVs = new Dictionary<long, (Vector2, Vector2)>();

                foreach (int t in island)
                {
                    for (int e = 0; e < 3; e++)
                    {
                        int v0 = triangles[t * 3 + e];
                        int v1 = triangles[t * 3 + (e + 1) % 3];
                        int lo = Mathf.Min(v0, v1), hi = Mathf.Max(v0, v1);
                        long key = (long)lo << 32 | (uint)hi;

                        edgeCount.TryGetValue(key, out int cnt);
                        edgeCount[key] = cnt + 1;
                        if (!edgeUVs.ContainsKey(key) && v0 < uvs.Length && v1 < uvs.Length)
                            edgeUVs[key] = (uvs[v0], uvs[v1]);
                    }
                }

                var boundary = new List<Vector2>();
                foreach (var kvp in edgeCount)
                {
                    if (kvp.Value == 1 && edgeUVs.TryGetValue(kvp.Key, out var edge))
                    {
                        boundary.Add(edge.Item1);
                        boundary.Add(edge.Item2);
                    }
                }
                islandBoundaries.Add(boundary);
            }

            // Find minimum distance between any two islands' boundary points
            float minDist = float.MaxValue;
            for (int i = 0; i < islandBoundaries.Count; i++)
            for (int j = i + 1; j < islandBoundaries.Count; j++)
            {
                // Sample to keep it fast
                var bA = islandBoundaries[i];
                var bB = islandBoundaries[j];
                int stepA = Mathf.Max(1, bA.Count / 200);
                int stepB = Mathf.Max(1, bB.Count / 200);

                for (int a = 0; a < bA.Count; a += stepA)
                for (int b = 0; b < bB.Count; b += stepB)
                {
                    float d = Vector2.Distance(bA[a], bB[b]);
                    if (d < minDist) minDist = d;
                }
            }

            return minDist * textureResolution;
        }

        #endregion

        #region Lightmap Validation

        public static LightmapValidation ValidateLightmapUVs(Mesh mesh, int textureResolution = 1024)
        {
            var result = new LightmapValidation
            {
                Issues = new List<string>()
            };

            var uvs = GetUVChannel(mesh, 1);
            result.HasUV1 = uvs.Length > 0;

            if (!result.HasUV1)
            {
                result.Issues.Add("Mesh has no UV1 channel (lightmap UVs).");
                return result;
            }

            // Coverage
            var triangles = mesh.triangles;
            float totalArea = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                totalArea += Mathf.Abs(Cross2D(uvs[i1] - uvs[i0], uvs[i2] - uvs[i0])) * 0.5f;
            }
            result.CoveragePercent = totalArea * 100f;

            // Out of bounds
            var oob = GetOutOfBoundsTriangles(mesh, 1);
            result.OutOfBoundsTriCount = oob.Count;
            if (oob.Count > 0)
                result.Issues.Add($"{oob.Count} triangles have UVs outside [0,1] range.");

            // Overlaps
            var overlaps = FindOverlappingTriangles(mesh, 1);
            result.OverlappingTriCount = overlaps.Count;
            if (overlaps.Count > 0)
                result.Issues.Add($"{overlaps.Count} overlapping triangles detected.");

            // Padding
            result.MinPaddingPixels = ComputeMinIslandPadding(mesh, 1, textureResolution);
            if (result.MinPaddingPixels < 2f)
                result.Issues.Add($"Minimum island padding is {result.MinPaddingPixels:F1}px (recommend >= 2px).");

            if (result.CoveragePercent < 10f)
                result.Issues.Add($"UV coverage is very low ({result.CoveragePercent:F1}%).");

            return result;
        }

        #endregion

        #region Overlap Detection

        public static HashSet<int> FindOverlappingTriangles(Mesh mesh, int uvChannel)
        {
            var uvs = GetUVChannel(mesh, uvChannel);
            var triangles = mesh.triangles;
            var result = new HashSet<int>();

            if (uvs.Length == 0) return result;

            int triCount = triangles.Length / 3;

            const int gridSize = 64;
            var grid = new Dictionary<int, List<int>>();

            var triUVs = new Vector2[triCount * 3];
            var triBounds = new (Vector2 min, Vector2 max)[triCount];

            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                Vector2 a = uvs[i0], b = uvs[i1], c = uvs[i2];
                triUVs[t * 3] = a;
                triUVs[t * 3 + 1] = b;
                triUVs[t * 3 + 2] = c;

                Vector2 mn = Vector2.Min(a, Vector2.Min(b, c));
                Vector2 mx = Vector2.Max(a, Vector2.Max(b, c));
                triBounds[t] = (mn, mx);

                int x0 = Mathf.Clamp(Mathf.FloorToInt(mn.x * gridSize), 0, gridSize - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(mn.y * gridSize), 0, gridSize - 1);
                int x1 = Mathf.Clamp(Mathf.FloorToInt(mx.x * gridSize), 0, gridSize - 1);
                int y1 = Mathf.Clamp(Mathf.FloorToInt(mx.y * gridSize), 0, gridSize - 1);

                for (int gx = x0; gx <= x1; gx++)
                for (int gy = y0; gy <= y1; gy++)
                {
                    int key = gx + gy * gridSize;
                    if (!grid.TryGetValue(key, out var list))
                    {
                        list = new List<int>();
                        grid[key] = list;
                    }
                    list.Add(t);
                }
            }

            foreach (var cell in grid.Values)
            {
                for (int i = 0; i < cell.Count; i++)
                for (int j = i + 1; j < cell.Count; j++)
                {
                    int tA = cell[i], tB = cell[j];
                    if (result.Contains(tA) && result.Contains(tB)) continue;
                    if (!BoundsOverlap(triBounds[tA], triBounds[tB])) continue;

                    if (TrianglesOverlap2D(
                            triUVs[tA * 3], triUVs[tA * 3 + 1], triUVs[tA * 3 + 2],
                            triUVs[tB * 3], triUVs[tB * 3 + 1], triUVs[tB * 3 + 2]))
                    {
                        result.Add(tA);
                        result.Add(tB);
                    }
                }
            }

            return result;
        }

        #endregion

        #region Geometry Helpers

        static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        static void Union(int[] parent, int[] rank, int a, int b)
        {
            a = Find(parent, a);
            b = Find(parent, b);
            if (a == b) return;
            if (rank[a] < rank[b]) (a, b) = (b, a);
            parent[b] = a;
            if (rank[a] == rank[b]) rank[a]++;
        }

        static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool BoundsOverlap((Vector2 min, Vector2 max) a, (Vector2 min, Vector2 max) b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x &&
                   a.min.y <= b.max.y && a.max.y >= b.min.y;
        }

        static bool TrianglesOverlap2D(Vector2 a0, Vector2 a1, Vector2 a2,
                                        Vector2 b0, Vector2 b1, Vector2 b2)
        {
            if (PointInTriangle(a0, b0, b1, b2) || PointInTriangle(a1, b0, b1, b2) ||
                PointInTriangle(a2, b0, b1, b2))
                return true;
            if (PointInTriangle(b0, a0, a1, a2) || PointInTriangle(b1, a0, a1, a2) ||
                PointInTriangle(b2, a0, a1, a2))
                return true;

            Vector2[] edgesA = { a0, a1, a1, a2, a2, a0 };
            Vector2[] edgesB = { b0, b1, b1, b2, b2, b0 };
            for (int i = 0; i < 6; i += 2)
            for (int j = 0; j < 6; j += 2)
            {
                if (SegmentsIntersect(edgesA[i], edgesA[i + 1], edgesB[j], edgesB[j + 1]))
                    return true;
            }

            return false;
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2D(b - a, p - a);
            float d2 = Cross2D(c - b, p - b);
            float d3 = Cross2D(a - c, p - c);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Vector2 d1 = p2 - p1, d2 = p4 - p3;
            float cross = Cross2D(d1, d2);
            if (Mathf.Abs(cross) < 1e-8f) return false;

            Vector2 d3 = p3 - p1;
            float t = Cross2D(d3, d2) / cross;
            float u = Cross2D(d3, d1) / cross;
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        #endregion
    }
}
#endif

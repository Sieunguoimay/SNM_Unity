#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.WaterSystemV2.Editor
{
    /// <summary>
    /// Generates a water surface mesh by running 2D marching squares on a
    /// terrain-height field. Ported unchanged from V1's WaterMeshGenerator
    /// (the algorithm was solid); only the surrounding workflow is new — see
    /// WaterBodyEditor for the Scan/Bake buttons that drive this.
    ///
    /// Pipeline:
    ///   1. Collect all terrain triangles and transform them into water-local space.
    ///   2. Extract shoreline segments via plane-triangle intersection.
    ///   3. Stitch shoreline segments into polylines with cumulative arc length (used for UV.y).
    ///   4. Build a regular grid over the water rect and classify each grid vertex as water or
    ///      land by testing whether any covering terrain triangle has interpolated Y > 0 at that
    ///      XZ position.
    ///   5. For each bimodal grid edge, find the exact intersection with a shoreline segment.
    ///   6. Emit marching squares triangles per cell via a lookup table. A per-triangle winding
    ///      helper ensures all faces are front-facing from above.
    ///
    /// Per-vertex UVs:
    ///   UV0   = normalized local rect [0,1]   — proper layout visible in any UV tool.
    ///   UV1.x = distance / maxShoreDistance   — 0 at shore, clamped to 1 in deep water.
    ///   UV1.y = arcLength / polylineLength * tiling — along-shore coordinate normalized per
    ///           polyline so foam textures wrap seamlessly around closed loops.
    /// </summary>
    public static class WaterShoreBaker
    {
        public class Input
        {
            public Transform WaterTransform;
            public List<GameObject> TerrainObjects;
            public Vector2 WaterSize = new(30f, 30f);
            public float GridCellSize = 1f;
            public float MaxShoreDistance = 4f;
            public float AlongShoreTiling = 1f;
        }

        public class Result
        {
            public Mesh Mesh;
            public List<Vector2> ShorelineSegmentPoints = new(); // flat pairs for visualization
            public int GridCols;
            public int GridRows;
            public Vector2[] GridPositions;
            public bool[] GridIsWater;
            public float[] GridDistance;
            public int VertexCount;
            public int TriangleCount;
            public int PolylineCount;
            public string Log = string.Empty;
        }

        private struct TerrainTri
        {
            public Vector3 A, B, C;
        }

        private struct RawSegment
        {
            public Vector2 A, B;
        }

        private class Polyline2D
        {
            public Vector2[] Points;
            public float[] SegStartArc; // per vertex: arc length from P0 walking the polyline
            public int SegCount;        // closed ? n : n-1
            public float TotalLength;
            public bool Closed;
        }

        // Vertex-kind IDs for the marching squares case table.
        private const int KBL = 0, KBR = 1, KTR = 2, KTL = 3;
        private const int KBOT = 4, KRIGHT = 5, KTOP = 6, KLEFT = 7;

        private static readonly int[][] CaseTriangles = new int[16][]
        {
            new int[0],                                                            // 0
            new[] { KBL, KBOT, KLEFT },                                            // 1
            new[] { KBR, KRIGHT, KBOT },                                           // 2
            new[] { KBL, KBR, KRIGHT,  KBL, KRIGHT, KLEFT },                       // 3
            new[] { KTR, KTOP, KRIGHT },                                           // 4
            new[] { KBL, KBOT, KLEFT,  KTR, KTOP, KRIGHT },                        // 5 (saddle)
            new[] { KBR, KTR, KTOP,  KBR, KTOP, KBOT },                            // 6
            new[] { KBL, KBR, KTR,  KBL, KTR, KTOP,  KBL, KTOP, KLEFT },           // 7
            new[] { KTL, KLEFT, KTOP },                                            // 8
            new[] { KBL, KBOT, KTOP,  KBL, KTOP, KTL },                            // 9
            new[] { KBR, KRIGHT, KBOT,  KTL, KLEFT, KTOP },                        // 10 (saddle)
            new[] { KBL, KBR, KRIGHT,  KBL, KRIGHT, KTOP,  KBL, KTOP, KTL },       // 11
            new[] { KTL, KTR, KRIGHT,  KTL, KRIGHT, KLEFT },                       // 12
            new[] { KBL, KBOT, KRIGHT,  KBL, KRIGHT, KTR,  KBL, KTR, KTL },        // 13
            new[] { KBOT, KBR, KTR,  KBOT, KTR, KTL,  KBOT, KTL, KLEFT },          // 14
            new[] { KBL, KBR, KTR,  KBL, KTR, KTL },                               // 15
        };

        private const float PlaneNudge = 1e-5f;
        private const float StitchEpsilon = 0.001f;

        public static Result Generate(Input input)
        {
            var result = new Result();

            if (input.WaterTransform == null)
            {
                result.Log = "Water transform is null.";
                return result;
            }
            if (input.GridCellSize < 0.01f || input.WaterSize.x <= 0.01f || input.WaterSize.y <= 0.01f)
            {
                result.Log = "Invalid grid cell size or water size.";
                return result;
            }

            var terrainTris = ExtractTerrainTriangles(input, out int skipped);
            var segments = ExtractShorelineSegments(terrainTris);
            var polylines = StitchPolylines(segments);
            result.PolylineCount = polylines.Count;

            result.ShorelineSegmentPoints = new List<Vector2>(segments.Count * 2);
            foreach (var pl in polylines)
            {
                int n = pl.Points.Length;
                for (int i = 0; i < pl.SegCount; i++)
                {
                    result.ShorelineSegmentPoints.Add(pl.Points[i]);
                    result.ShorelineSegmentPoints.Add(pl.Points[(i + 1) % n]);
                }
            }

            var h = input.WaterSize * 0.5f;
            int cols = Mathf.Max(2, Mathf.CeilToInt(input.WaterSize.x / input.GridCellSize) + 1);
            int rows = Mathf.Max(2, Mathf.CeilToInt(input.WaterSize.y / input.GridCellSize) + 1);
            float stepX = input.WaterSize.x / (cols - 1);
            float stepZ = input.WaterSize.y / (rows - 1);
            int total = cols * rows;

            var gridPos = new Vector2[total];
            var gridIsWater = new bool[total];
            var gridDist = new float[total];
            float maxShoreDist = Mathf.Max(0.0001f, input.MaxShoreDistance);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int gi = r * cols + c;
                    float x = -h.x + c * stepX;
                    float z = -h.y + r * stepZ;
                    var p = new Vector2(x, z);
                    gridPos[gi] = p;
                    gridIsWater[gi] = IsGridVertexWater(p, terrainTris);
                    QueryShoreline(p, polylines, out float d, out _);
                    gridDist[gi] = polylines.Count > 0 ? d : maxShoreDist;
                }
            }

            result.GridCols = cols;
            result.GridRows = rows;
            result.GridPositions = gridPos;
            result.GridIsWater = gridIsWater;
            result.GridDistance = gridDist;

            result.Mesh = BuildMarchingSquaresMesh(input, cols, rows, h,
                gridPos, gridIsWater, polylines, maxShoreDist,
                out int vCount, out int tCount);
            result.VertexCount = vCount;
            result.TriangleCount = tCount;

            if (vCount == 0)
            {
                result.Log = "No mesh generated (no water in the rect).";
                return result;
            }

            result.Log = $"OK. grid={cols}x{rows} verts={vCount} tris={tCount} " +
                         $"polylines={polylines.Count}" +
                         (skipped > 0 ? $" (skipped {skipped} mesh(es))" : string.Empty);

            var sc = input.WaterTransform.lossyScale;
            if (Mathf.Abs(sc.x - 1f) > 0.001f || Mathf.Abs(sc.y - 1f) > 0.001f || Mathf.Abs(sc.z - 1f) > 0.001f)
            {
                result.Log += $"\nNote: water transform lossy scale is {sc}; parameters are in water-local units.";
            }

            return result;
        }

        // ── Step 1: gather terrain triangles in water-local space ──

        private static List<TerrainTri> ExtractTerrainTriangles(Input input, out int skippedMeshes)
        {
            skippedMeshes = 0;
            var result = new List<TerrainTri>();
            if (input.TerrainObjects == null) return result;

            var waterT = input.WaterTransform;
            foreach (var go in input.TerrainObjects)
            {
                if (go == null) continue;
                var filters = go.GetComponentsInChildren<MeshFilter>(includeInactive: true);
                foreach (var mf in filters)
                {
                    var mesh = mf.sharedMesh;
                    if (mesh == null) continue;
                    try
                    {
                        AppendTerrainTris(mesh, mf.transform, waterT, result);
                    }
                    catch (System.Exception e)
                    {
                        skippedMeshes++;
                        Debug.LogWarning($"[WaterShoreBaker] Failed to read mesh '{mesh.name}': {e.Message}");
                    }
                }
            }
            return result;
        }

        private static void AppendTerrainTris(Mesh mesh, Transform meshT, Transform waterT, List<TerrainTri> output)
        {
            using var dataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var data = dataArray[0];
            int vc = data.vertexCount;
            if (vc == 0) return;

            var localVerts = new Vector3[vc];
            using (var verts = new NativeArray<Vector3>(vc, Allocator.Temp))
            {
                data.GetVertices(verts);
                for (int i = 0; i < vc; i++)
                {
                    var world = meshT.TransformPoint(verts[i]);
                    var local = waterT.InverseTransformPoint(world);
                    if (Mathf.Abs(local.y) < PlaneNudge) local.y = PlaneNudge;
                    localVerts[i] = local;
                }
            }

            int subCount = data.subMeshCount;
            for (int s = 0; s < subCount; s++)
            {
                var sub = data.GetSubMesh(s);
                if (sub.topology != MeshTopology.Triangles) continue;
                int idxCount = (int)sub.indexCount;
                int[] indices;
                using (var idx = new NativeArray<int>(idxCount, Allocator.Temp))
                {
                    data.GetIndices(idx, s);
                    indices = idx.ToArray();
                }
                for (int t = 0; t < indices.Length; t += 3)
                {
                    output.Add(new TerrainTri
                    {
                        A = localVerts[indices[t]],
                        B = localVerts[indices[t + 1]],
                        C = localVerts[indices[t + 2]]
                    });
                }
            }
        }

        // ── Step 2: extract shoreline segments ──

        private static List<RawSegment> ExtractShorelineSegments(List<TerrainTri> tris)
        {
            var result = new List<RawSegment>();
            for (int i = 0; i < tris.Count; i++)
            {
                var t = tris[i];
                if (!TryTriangleCrossingPlane(t.A, t.B, t.C, out Vector3 p0, out Vector3 p1)) continue;
                var segA = new Vector2(p0.x, p0.z);
                var segB = new Vector2(p1.x, p1.z);
                if ((segA - segB).sqrMagnitude < 1e-10f) continue;
                result.Add(new RawSegment { A = segA, B = segB });
            }
            return result;
        }

        private static bool TryTriangleCrossingPlane(Vector3 a, Vector3 b, Vector3 c,
            out Vector3 p0, out Vector3 p1)
        {
            p0 = default; p1 = default;
            int above = (a.y > 0f ? 1 : 0) + (b.y > 0f ? 1 : 0) + (c.y > 0f ? 1 : 0);
            int below = (a.y < 0f ? 1 : 0) + (b.y < 0f ? 1 : 0) + (c.y < 0f ? 1 : 0);
            if (above == 0 || below == 0) return false;

            Vector3 x0 = default, x1 = default;
            bool has0 = false, has1 = false;
            TryEdgeCross(a, b, ref x0, ref x1, ref has0, ref has1);
            TryEdgeCross(b, c, ref x0, ref x1, ref has0, ref has1);
            TryEdgeCross(c, a, ref x0, ref x1, ref has0, ref has1);
            if (!has0 || !has1) return false;
            p0 = x0; p1 = x1;
            return true;
        }

        private static void TryEdgeCross(Vector3 v0, Vector3 v1,
            ref Vector3 x0, ref Vector3 x1, ref bool has0, ref bool has1)
        {
            if ((v0.y > 0f && v1.y < 0f) || (v0.y < 0f && v1.y > 0f))
            {
                float t = v0.y / (v0.y - v1.y);
                var p = v0 + (v1 - v0) * t;
                if (!has0) { x0 = p; has0 = true; }
                else if (!has1) { x1 = p; has1 = true; }
            }
        }

        // ── Step 3: stitch segments into polylines with arc length ──

        private static List<Polyline2D> StitchPolylines(List<RawSegment> segments)
        {
            var result = new List<Polyline2D>();
            int n = segments.Count;
            if (n == 0) return result;

            var endpointToSegs = new Dictionary<long, List<int>>();
            for (int i = 0; i < n; i++)
            {
                AddEndpoint(endpointToSegs, Quantize(segments[i].A), i);
                AddEndpoint(endpointToSegs, Quantize(segments[i].B), i);
            }

            var used = new bool[n];
            for (int seed = 0; seed < n; seed++)
            {
                if (used[seed]) continue;
                used[seed] = true;

                var points = new List<Vector2> { segments[seed].A, segments[seed].B };
                WalkDirection(segments, endpointToSegs, used, points, appendToEnd: true);
                WalkDirection(segments, endpointToSegs, used, points, appendToEnd: false);

                bool closed = points.Count >= 3 &&
                              (points[0] - points[^1]).sqrMagnitude < StitchEpsilon * StitchEpsilon;
                if (closed) points.RemoveAt(points.Count - 1);
                if (points.Count < 2) continue;

                result.Add(BuildPolyline2D(points.ToArray(), closed));
            }

            return result;
        }

        private static Polyline2D BuildPolyline2D(Vector2[] points, bool closed)
        {
            int n = points.Length;
            int segCount = closed ? n : n - 1;
            var segStart = new float[n];
            float running = 0f;
            for (int i = 0; i < n; i++)
            {
                segStart[i] = running;
                if (!closed && i == n - 1) break;
                int next = (i + 1) % n;
                running += Vector2.Distance(points[i], points[next]);
            }
            return new Polyline2D
            {
                Points = points,
                SegStartArc = segStart,
                SegCount = segCount,
                TotalLength = running,
                Closed = closed
            };
        }

        private static void WalkDirection(List<RawSegment> segs, Dictionary<long, List<int>> map,
            bool[] used, List<Vector2> points, bool appendToEnd)
        {
            float epsSq = StitchEpsilon * StitchEpsilon;
            while (true)
            {
                Vector2 tip = appendToEnd ? points[^1] : points[0];
                long key = Quantize(tip);
                if (!map.TryGetValue(key, out var candidates)) break;

                int nextSeg = -1;
                foreach (int idx in candidates)
                {
                    if (!used[idx]) { nextSeg = idx; break; }
                }
                if (nextSeg < 0) break;

                used[nextSeg] = true;
                var seg = segs[nextSeg];
                Vector2 other = (seg.A - tip).sqrMagnitude < epsSq ? seg.B : seg.A;

                if (appendToEnd) points.Add(other);
                else points.Insert(0, other);
            }
        }

        private static long Quantize(Vector2 p)
        {
            int x = Mathf.RoundToInt(p.x / StitchEpsilon);
            int y = Mathf.RoundToInt(p.y / StitchEpsilon);
            return ((long)x << 32) ^ (uint)y;
        }

        private static void AddEndpoint(Dictionary<long, List<int>> map, long key, int segIdx)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<int>();
                map[key] = list;
            }
            list.Add(segIdx);
        }

        // ── Step 4: classify grid vertices ──

        private static bool IsGridVertexWater(Vector2 p, List<TerrainTri> tris)
        {
            float maxY = float.MinValue;
            bool anyCovers = false;
            for (int i = 0; i < tris.Count; i++)
            {
                if (PointInTriangleXZ(p, tris[i], out float y))
                {
                    if (!anyCovers || y > maxY)
                    {
                        maxY = y;
                        anyCovers = true;
                    }
                }
            }
            if (!anyCovers) return true;
            return maxY <= 0f;
        }

        private static bool PointInTriangleXZ(Vector2 p, TerrainTri t, out float y)
        {
            y = 0f;
            float x0 = t.A.x, z0 = t.A.z;
            float x1 = t.B.x, z1 = t.B.z;
            float x2 = t.C.x, z2 = t.C.z;

            float denom = (z1 - z2) * (x0 - x2) + (x2 - x1) * (z0 - z2);
            if (Mathf.Abs(denom) < 1e-10f) return false;

            float a = ((z1 - z2) * (p.x - x2) + (x2 - x1) * (p.y - z2)) / denom;
            float b = ((z2 - z0) * (p.x - x2) + (x0 - x2) * (p.y - z2)) / denom;
            float cBary = 1f - a - b;

            if (a < 0f || b < 0f || cBary < 0f) return false;
            y = a * t.A.y + b * t.B.y + cBary * t.C.y;
            return true;
        }

        // ── Shoreline field query: nearest point, distance, along-shore ──

        private static void QueryShoreline(Vector2 p, List<Polyline2D> polylines,
            out float distance, out float arcNormalized)
        {
            float bestDistSq = float.MaxValue;
            float bestArc = 0f;
            float bestTotal = 1f;

            for (int pi = 0; pi < polylines.Count; pi++)
            {
                var pl = polylines[pi];
                int n = pl.Points.Length;
                for (int i = 0; i < pl.SegCount; i++)
                {
                    var a = pl.Points[i];
                    var b = pl.Points[(i + 1) % n];
                    var ab = b - a;
                    float lenSq = ab.sqrMagnitude;
                    if (lenSq < 1e-10f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                    var closest = a + ab * t;
                    float dSq = (p - closest).sqrMagnitude;
                    if (dSq < bestDistSq)
                    {
                        bestDistSq = dSq;
                        float segLen = Mathf.Sqrt(lenSq);
                        bestArc = pl.SegStartArc[i] + t * segLen;
                        bestTotal = pl.TotalLength;
                    }
                }
            }

            if (bestDistSq == float.MaxValue)
            {
                distance = 0f;
                arcNormalized = 0f;
                return;
            }
            distance = Mathf.Sqrt(bestDistSq);
            arcNormalized = bestTotal > 1e-6f ? bestArc / bestTotal : 0f;
        }

        // ── Step 5/6: marching squares mesh ──

        private static Mesh BuildMarchingSquaresMesh(Input input,
            int cols, int rows, Vector2 halfSize,
            Vector2[] gridPos, bool[] gridIsWater,
            List<Polyline2D> polylines, float maxShoreDist,
            out int vertexCount, out int triangleCount)
        {
            float invW = 1f / input.WaterSize.x;
            float invH = 1f / input.WaterSize.y;
            float alongTiling = input.AlongShoreTiling;

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var triangles = new List<int>();

            var gridIdx = new int[cols * rows];
            for (int i = 0; i < cols * rows; i++)
            {
                if (gridIsWater[i])
                {
                    gridIdx[i] = positions.Count;
                    QueryShoreline(gridPos[i], polylines, out float dist, out float arc);
                    if (polylines.Count == 0) dist = maxShoreDist;
                    AddVertex(positions, normals, uv0, uv1, gridPos[i], dist, arc,
                        maxShoreDist, halfSize, invW, invH, alongTiling);
                }
                else
                {
                    gridIdx[i] = -1;
                }
            }

            int hSize = (cols - 1) * rows;
            int vSize = cols * (rows - 1);
            var hCrossIdx = new int[hSize];
            var vCrossIdx = new int[vSize];
            for (int i = 0; i < hSize; i++) hCrossIdx[i] = -1;
            for (int i = 0; i < vSize; i++) vCrossIdx[i] = -1;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = r * cols + c + 1;
                    if (gridIsWater[i0] == gridIsWater[i1]) continue;
                    var cross = FindEdgeCrossing(gridPos[i0], gridPos[i1], polylines);
                    int hi = r * (cols - 1) + c;
                    hCrossIdx[hi] = positions.Count;
                    QueryShoreline(cross, polylines, out _, out float arc);
                    AddVertex(positions, normals, uv0, uv1, cross, 0f, arc,
                        maxShoreDist, halfSize, invW, invH, alongTiling);
                }
            }
            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = (r + 1) * cols + c;
                    if (gridIsWater[i0] == gridIsWater[i1]) continue;
                    var cross = FindEdgeCrossing(gridPos[i0], gridPos[i1], polylines);
                    int vi = r * cols + c;
                    vCrossIdx[vi] = positions.Count;
                    QueryShoreline(cross, polylines, out _, out float arc);
                    AddVertex(positions, normals, uv0, uv1, cross, 0f, arc,
                        maxShoreDist, halfSize, invW, invH, alongTiling);
                }
            }

            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int iBL = r * cols + c;
                    int iBR = r * cols + c + 1;
                    int iTR = (r + 1) * cols + c + 1;
                    int iTL = (r + 1) * cols + c;

                    int caseMask =
                        (gridIsWater[iBL] ? 1 : 0) |
                        (gridIsWater[iBR] ? 2 : 0) |
                        (gridIsWater[iTR] ? 4 : 0) |
                        (gridIsWater[iTL] ? 8 : 0);
                    if (caseMask == 0) continue;

                    int vBL = gridIdx[iBL];
                    int vBR = gridIdx[iBR];
                    int vTR = gridIdx[iTR];
                    int vTL = gridIdx[iTL];
                    int vBot = hCrossIdx[r * (cols - 1) + c];
                    int vTop = hCrossIdx[(r + 1) * (cols - 1) + c];
                    int vLeft = vCrossIdx[r * cols + c];
                    int vRight = vCrossIdx[r * cols + c + 1];

                    var tris = CaseTriangles[caseMask];
                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int a = ResolveKind(tris[t],     vBL, vBR, vTR, vTL, vBot, vRight, vTop, vLeft);
                        int b = ResolveKind(tris[t + 1], vBL, vBR, vTR, vTL, vBot, vRight, vTop, vLeft);
                        int cIdx = ResolveKind(tris[t + 2], vBL, vBR, vTR, vTL, vBot, vRight, vTop, vLeft);
                        if (a < 0 || b < 0 || cIdx < 0) continue;
                        AddTriangleCW(triangles, a, b, cIdx, positions);
                    }
                }
            }

            vertexCount = positions.Count;
            triangleCount = triangles.Count / 3;

            var mesh = new Mesh { name = "GeneratedWaterSurface" };
            mesh.indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int ResolveKind(int kind,
            int vBL, int vBR, int vTR, int vTL,
            int vBot, int vRight, int vTop, int vLeft)
        {
            return kind switch
            {
                KBL => vBL,
                KBR => vBR,
                KTR => vTR,
                KTL => vTL,
                KBOT => vBot,
                KRIGHT => vRight,
                KTOP => vTop,
                KLEFT => vLeft,
                _ => -1
            };
        }

        private static void AddVertex(List<Vector3> positions, List<Vector3> normals,
            List<Vector2> uv0, List<Vector2> uv1,
            Vector2 p2, float dist, float arcNormalized,
            float maxShoreDist, Vector2 halfSize,
            float invW, float invH, float alongShoreTiling)
        {
            positions.Add(new Vector3(p2.x, 0f, p2.y));
            normals.Add(Vector3.up);
            float u = Mathf.Clamp01(dist / maxShoreDist);
            uv0.Add(new Vector2((p2.x + halfSize.x) * invW, (p2.y + halfSize.y) * invH));
            uv1.Add(new Vector2(u, arcNormalized * alongShoreTiling));
        }

        private static Vector2 FindEdgeCrossing(Vector2 p0, Vector2 p1, List<Polyline2D> polylines)
        {
            for (int pi = 0; pi < polylines.Count; pi++)
            {
                var pl = polylines[pi];
                int n = pl.Points.Length;
                for (int i = 0; i < pl.SegCount; i++)
                {
                    var a = pl.Points[i];
                    var b = pl.Points[(i + 1) % n];
                    if (SegmentIntersectionPoint(p0, p1, a, b, out Vector2 hit)) return hit;
                }
            }
            return (p0 + p1) * 0.5f;
        }

        private static bool SegmentIntersectionPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit)
        {
            hit = default;
            var r = b - a;
            var s = d - c;
            float denom = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(denom) < 1e-10f) return false;
            var ac = c - a;
            float t = (ac.x * s.y - ac.y * s.x) / denom;
            float u = (ac.x * r.y - ac.y * r.x) / denom;
            if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
            hit = a + r * t;
            return true;
        }

        private static void AddTriangleCW(List<int> triangles, int a, int b, int c, List<Vector3> positions)
        {
            var pa = positions[a];
            var pb = positions[b];
            var pc = positions[c];
            float cross = (pb.x - pa.x) * (pc.z - pa.z) - (pb.z - pa.z) * (pc.x - pa.x);
            if (cross <= 0f)
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }
            else
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
            }
        }
    }
}
#endif

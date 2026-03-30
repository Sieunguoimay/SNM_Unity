#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.UVLayout
{
    public static class UVLayoutRenderer
    {
        static Material _lineMaterial;

        static Material LineMaterial
        {
            get
            {
                if (_lineMaterial == null)
                {
                    var shader = Shader.Find("Hidden/Internal-Colored");
                    _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _lineMaterial.SetInt("_ZWrite", 0);
                }
                return _lineMaterial;
            }
        }

        public struct RenderContext
        {
            public Mesh Mesh;
            public UVLayoutSettings Settings;
            public HashSet<int> OverlappingTris;
            public HashSet<int> OutOfBoundsTris;
            public List<List<int>> Islands;
            public Color[] IslandColors;
            public float[] TexelDensities;
            public List<(Vector2 a, Vector2 b)> SeamEdges;
            public float[,] VertexDensityMap;
            public float VertexDensityMax;
        }

        public static Texture2D Render(RenderContext ctx)
        {
            var mesh = ctx.Mesh;
            var settings = ctx.Settings;
            var uvs = UVLayoutAnalyzer.GetUVChannel(mesh, settings.uvChannel);
            if (uvs.Length == 0) return null;

            int res = settings.resolution;
            var rt = RenderTexture.GetTemporary(res, res, 0, RenderTextureFormat.ARGB32);
            var prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            Color bg = settings.transparentBackground ? Color.clear : settings.backgroundColor;
            GL.Clear(true, true, bg);

            LineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, res, 0, res);

            var triangles = mesh.triangles;
            int triCount = triangles.Length / 3;

            // Layer 1: Texture overlay
            if (settings.overlayTexture != null)
                DrawTextureOverlay(settings.overlayTexture, settings.overlayOpacity, res);

            // Layer 2: Vertex density heatmap
            if (settings.showVertexDensity && ctx.VertexDensityMap != null)
                DrawVertexDensity(ctx.VertexDensityMap, ctx.VertexDensityMax, res);

            // Layer 3: Grid (behind wireframe)
            if (settings.showGrid)
                DrawGrid(res, settings.gridSubdivisions, settings.gridColor);

            // Layer 3b: UDIM grid
            if (settings.showUDIM)
                DrawUDIMGrid(uvs, res);

            // Build submesh lookup
            int[] triSubmesh = null;
            Color[] submeshColors = null;
            if (settings.colorBySubmesh && mesh.subMeshCount > 1)
            {
                triSubmesh = BuildSubmeshLookup(mesh);
                submeshColors = GenerateSubmeshColors(mesh.subMeshCount);
            }

            // Build island lookup
            int[] triIsland = null;
            if (settings.colorByIsland && ctx.Islands != null)
            {
                triIsland = new int[triCount];
                for (int i = -1; ++i < triCount;) triIsland[i] = -1;
                for (int i = 0; i < ctx.Islands.Count; i++)
                    foreach (int t in ctx.Islands[i])
                        triIsland[t] = i;
            }

            // Layer 4: Triangle fill
            if (settings.showFill || settings.showTexelDensity)
            {
                GL.Begin(GL.TRIANGLES);
                for (int t = 0; t < triCount; t++)
                {
                    int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                    Color col;
                    if (settings.showTexelDensity && ctx.TexelDensities != null)
                    {
                        col = UVLayoutAnalyzer.DensityToColor(
                            ctx.TexelDensities[t], settings.texelDensityMin, settings.texelDensityMax);
                        col.a = 0.7f;
                    }
                    else if (triIsland != null && triIsland[t] >= 0 && ctx.IslandColors != null)
                    {
                        col = ctx.IslandColors[triIsland[t]];
                        col.a = 0.5f;
                    }
                    else if (triSubmesh != null)
                    {
                        col = submeshColors[triSubmesh[t]];
                        col.a = settings.fillColor.a;
                    }
                    else
                    {
                        col = settings.fillColor;
                    }

                    GL.Color(col);
                    GL.Vertex3(uvs[i0].x * res, uvs[i0].y * res, 0);
                    GL.Vertex3(uvs[i1].x * res, uvs[i1].y * res, 0);
                    GL.Vertex3(uvs[i2].x * res, uvs[i2].y * res, 0);
                }
                GL.End();
            }

            // Layer 5: Overlap highlight
            if (settings.highlightOverlaps && ctx.OverlappingTris != null && ctx.OverlappingTris.Count > 0)
            {
                GL.Begin(GL.TRIANGLES);
                GL.Color(settings.overlapColor);
                foreach (int t in ctx.OverlappingTris)
                {
                    int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                    GL.Vertex3(uvs[i0].x * res, uvs[i0].y * res, 0);
                    GL.Vertex3(uvs[i1].x * res, uvs[i1].y * res, 0);
                    GL.Vertex3(uvs[i2].x * res, uvs[i2].y * res, 0);
                }
                GL.End();
            }

            // Layer 6: Out-of-bounds highlight
            if (settings.highlightOutOfBounds && ctx.OutOfBoundsTris != null && ctx.OutOfBoundsTris.Count > 0)
            {
                GL.Begin(GL.TRIANGLES);
                GL.Color(settings.outOfBoundsColor);
                foreach (int t in ctx.OutOfBoundsTris)
                {
                    int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                    GL.Vertex3(uvs[i0].x * res, uvs[i0].y * res, 0);
                    GL.Vertex3(uvs[i1].x * res, uvs[i1].y * res, 0);
                    GL.Vertex3(uvs[i2].x * res, uvs[i2].y * res, 0);
                }
                GL.End();
            }

            // Layer 7: Wireframe
            GL.Begin(GL.LINES);
            for (int t = 0; t < triCount; t++)
            {
                int i0 = triangles[t * 3], i1 = triangles[t * 3 + 1], i2 = triangles[t * 3 + 2];
                if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                Color col = settings.lineColor;
                if (triIsland != null && triIsland[t] >= 0 && ctx.IslandColors != null)
                    col = ctx.IslandColors[triIsland[t]];
                else if (triSubmesh != null)
                    col = submeshColors[triSubmesh[t]];

                GL.Color(col);
                Vector3 a = new(uvs[i0].x * res, uvs[i0].y * res, 0);
                Vector3 b = new(uvs[i1].x * res, uvs[i1].y * res, 0);
                Vector3 c = new(uvs[i2].x * res, uvs[i2].y * res, 0);

                GL.Vertex(a); GL.Vertex(b);
                GL.Vertex(b); GL.Vertex(c);
                GL.Vertex(c); GL.Vertex(a);
            }
            GL.End();

            // Layer 8: Seam edges
            if (settings.showSeams && ctx.SeamEdges != null && ctx.SeamEdges.Count > 0)
            {
                GL.Begin(GL.LINES);
                GL.Color(settings.seamColor);
                foreach (var (a, b) in ctx.SeamEdges)
                {
                    GL.Vertex3(a.x * res, a.y * res, 0);
                    GL.Vertex3(b.x * res, b.y * res, 0);
                }
                GL.End();
            }

            GL.PopMatrix();

            // Read back
            var tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
            tex.Apply();

            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }

        // Simplified overload for batch export / backward compat
        public static Texture2D Render(Mesh mesh, UVLayoutSettings settings,
            HashSet<int> overlappingTris = null)
        {
            return Render(new RenderContext
            {
                Mesh = mesh,
                Settings = settings,
                OverlappingTris = overlappingTris
            });
        }

        #region Drawing Helpers

        static void DrawTextureOverlay(Texture2D tex, float opacity, int res)
        {
            GL.End(); // ensure no active batch
            GL.PushMatrix();

            // Draw the texture as a fullscreen quad
            var mat = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = tex
            };
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            // Fallback: draw as colored quads sampling the texture
            // Since Internal-Colored doesn't support textures well, we blit manually
            GL.PopMatrix();

            var prevActive = RenderTexture.active;
            // We're already rendering to RT, just draw the texture overlay as a quad
            Graphics.DrawTexture(new Rect(0, 0, res, res), tex,
                new Rect(0, 0, 1, 1), 0, 0, 0, 0,
                new Color(1, 1, 1, opacity));

            LineMaterial.SetPass(0);
        }

        static void DrawVertexDensity(float[,] densityMap, float maxDensity, int res)
        {
            if (maxDensity <= 0) return;

            int mapRes = densityMap.GetLength(0);
            // Draw as colored quads at lower resolution for performance
            int blockSize = Mathf.Max(1, res / mapRes);

            GL.Begin(GL.QUADS);
            for (int y = 0; y < mapRes; y++)
            for (int x = 0; x < mapRes; x++)
            {
                float val = densityMap[x, y];
                if (val <= 0) continue;

                float t = val / maxDensity;
                Color col = UVLayoutAnalyzer.DensityToColor(t, 0, 1);
                col.a = t * 0.6f;
                GL.Color(col);

                float px = x * blockSize, py = y * blockSize;
                GL.Vertex3(px, py, 0);
                GL.Vertex3(px + blockSize, py, 0);
                GL.Vertex3(px + blockSize, py + blockSize, 0);
                GL.Vertex3(px, py + blockSize, 0);
            }
            GL.End();
        }

        static void DrawGrid(int res, int subdivisions, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            GL.Vertex3(0, 0, 0); GL.Vertex3(res, 0, 0);
            GL.Vertex3(res, 0, 0); GL.Vertex3(res, res, 0);
            GL.Vertex3(res, res, 0); GL.Vertex3(0, res, 0);
            GL.Vertex3(0, res, 0); GL.Vertex3(0, 0, 0);

            if (subdivisions > 1)
            {
                float step = res / (float)subdivisions;
                for (int i = 1; i < subdivisions; i++)
                {
                    float pos = i * step;
                    GL.Vertex3(pos, 0, 0); GL.Vertex3(pos, res, 0);
                    GL.Vertex3(0, pos, 0); GL.Vertex3(res, pos, 0);
                }
            }
            GL.End();
        }

        static void DrawUDIMGrid(Vector2[] uvs, int res)
        {
            // Find UV range
            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            foreach (var uv in uvs)
            {
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }

            int tileMinX = Mathf.FloorToInt(min.x);
            int tileMaxX = Mathf.CeilToInt(max.x);
            int tileMinY = Mathf.FloorToInt(min.y);
            int tileMaxY = Mathf.CeilToInt(max.y);

            GL.Begin(GL.LINES);
            GL.Color(new Color(0.5f, 0.5f, 0f, 0.6f));

            for (int x = tileMinX; x <= tileMaxX; x++)
            {
                float px = x * res;
                GL.Vertex3(px, tileMinY * res, 0);
                GL.Vertex3(px, tileMaxY * res, 0);
            }
            for (int y = tileMinY; y <= tileMaxY; y++)
            {
                float py = y * res;
                GL.Vertex3(tileMinX * res, py, 0);
                GL.Vertex3(tileMaxX * res, py, 0);
            }
            GL.End();
        }

        static int[] BuildSubmeshLookup(Mesh mesh)
        {
            int triCount = mesh.triangles.Length / 3;
            var lookup = new int[triCount];
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                var desc = mesh.GetSubMesh(sm);
                int start = desc.indexStart / 3;
                int count = desc.indexCount / 3;
                for (int t = start; t < start + count; t++)
                    lookup[t] = sm;
            }
            return lookup;
        }

        static Color[] GenerateSubmeshColors(int count)
        {
            var colors = new Color[count];
            for (int i = 0; i < count; i++)
                colors[i] = Color.HSVToRGB((float)i / count, 0.7f, 1f);
            return colors;
        }

        #endregion
    }
}
#endif

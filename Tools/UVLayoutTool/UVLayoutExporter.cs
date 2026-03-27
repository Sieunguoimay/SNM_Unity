#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.UVLayoutTool
{
    public static class UVLayoutExporter
    {
        #region PNG

        public static string ExportToPNG(Texture2D texture, string defaultName = "uv_layout")
        {
            string path = EditorUtility.SaveFilePanel("Export UV Layout", "", defaultName, "png");
            if (string.IsNullOrEmpty(path)) return null;

            File.WriteAllBytes(path, texture.EncodeToPNG());

            if (path.StartsWith(Application.dataPath))
                AssetDatabase.Refresh();

            return path;
        }

        public static int BatchExport(Mesh[] meshes, UVLayoutSettings settings)
        {
            string folder = EditorUtility.SaveFolderPanel("Batch Export UV Layouts", "", "");
            if (string.IsNullOrEmpty(folder)) return 0;

            int exported = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                if (mesh == null) continue;

                EditorUtility.DisplayProgressBar("Batch Export",
                    $"Exporting {mesh.name}... ({i + 1}/{meshes.Length})",
                    (float)i / meshes.Length);

                var tex = UVLayoutRenderer.Render(mesh, settings);
                if (tex == null) continue;

                string fileName = $"{SanitizeFileName(mesh.name)}_uv{settings.uvChannel}.png";
                File.WriteAllBytes(Path.Combine(folder, fileName), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                exported++;
            }

            EditorUtility.ClearProgressBar();

            if (folder.StartsWith(Application.dataPath))
                AssetDatabase.Refresh();

            return exported;
        }

        #endregion

        #region SVG

        public static string ExportToSVG(Mesh mesh, UVLayoutSettings settings, string defaultName = "uv_layout")
        {
            string path = EditorUtility.SaveFilePanel("Export UV Layout as SVG", "", defaultName, "svg");
            if (string.IsNullOrEmpty(path)) return null;

            var uvs = UVLayoutAnalyzer.GetUVChannel(mesh, settings.uvChannel);
            if (uvs.Length == 0) return null;

            int res = settings.resolution;
            var triangles = mesh.triangles;
            var sb = new StringBuilder();

            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{res}\" height=\"{res}\" viewBox=\"0 0 {res} {res}\">");

            // Background
            if (!settings.transparentBackground)
            {
                string bgHex = ColorToHex(settings.backgroundColor);
                sb.AppendLine($"  <rect width=\"{res}\" height=\"{res}\" fill=\"{bgHex}\"/>");
            }

            // Grid
            if (settings.showGrid && settings.gridSubdivisions > 1)
            {
                string gridHex = ColorToHex(settings.gridColor);
                sb.AppendLine($"  <g stroke=\"{gridHex}\" stroke-width=\"1\" fill=\"none\">");
                sb.AppendLine($"    <rect x=\"0\" y=\"0\" width=\"{res}\" height=\"{res}\"/>");
                float step = res / (float)settings.gridSubdivisions;
                for (int i = 1; i < settings.gridSubdivisions; i++)
                {
                    string pos = F(i * step);
                    sb.AppendLine($"    <line x1=\"{pos}\" y1=\"0\" x2=\"{pos}\" y2=\"{res}\"/>");
                    sb.AppendLine($"    <line x1=\"0\" y1=\"{pos}\" x2=\"{res}\" y2=\"{pos}\"/>");
                }
                sb.AppendLine("  </g>");
            }

            // Triangle fills
            if (settings.showFill)
            {
                string fillHex = ColorToHex(settings.fillColor);
                string fillOpacity = F(settings.fillColor.a);
                sb.AppendLine($"  <g fill=\"{fillHex}\" fill-opacity=\"{fillOpacity}\" stroke=\"none\">");
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                    sb.AppendLine(TriToSVGPolygon(uvs[i0], uvs[i1], uvs[i2], res));
                }
                sb.AppendLine("  </g>");
            }

            // Wireframe
            {
                string lineHex = ColorToHex(settings.lineColor);
                string lineW = F(Mathf.Max(0.5f, settings.lineWidth));
                sb.AppendLine($"  <g stroke=\"{lineHex}\" stroke-width=\"{lineW}\" fill=\"none\">");
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int i0 = triangles[t], i1 = triangles[t + 1], i2 = triangles[t + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;
                    sb.AppendLine(TriToSVGPolyline(uvs[i0], uvs[i1], uvs[i2], res));
                }
                sb.AppendLine("  </g>");
            }

            // Seam edges
            if (settings.showSeams)
            {
                var seams = UVLayoutAnalyzer.FindSeamEdges(mesh, settings.uvChannel);
                if (seams.Count > 0)
                {
                    string seamHex = ColorToHex(settings.seamColor);
                    sb.AppendLine($"  <g stroke=\"{seamHex}\" stroke-width=\"2\" fill=\"none\">");
                    foreach (var (a, b) in seams)
                    {
                        sb.AppendLine($"    <line x1=\"{F(a.x * res)}\" y1=\"{F((1 - a.y) * res)}\" " +
                                      $"x2=\"{F(b.x * res)}\" y2=\"{F((1 - b.y) * res)}\"/>");
                    }
                    sb.AppendLine("  </g>");
                }
            }

            sb.AppendLine("</svg>");

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        static string TriToSVGPolygon(Vector2 a, Vector2 b, Vector2 c, int res)
        {
            // SVG Y is inverted relative to UV
            return $"    <polygon points=\"{F(a.x * res)},{F((1 - a.y) * res)} " +
                   $"{F(b.x * res)},{F((1 - b.y) * res)} " +
                   $"{F(c.x * res)},{F((1 - c.y) * res)}\"/>";
        }

        static string TriToSVGPolyline(Vector2 a, Vector2 b, Vector2 c, int res)
        {
            return $"    <polygon points=\"{F(a.x * res)},{F((1 - a.y) * res)} " +
                   $"{F(b.x * res)},{F((1 - b.y) * res)} " +
                   $"{F(c.x * res)},{F((1 - c.y) * res)}\"/>";
        }

        #endregion

        #region Helpers

        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        static string ColorToHex(Color c)
        {
            return $"#{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
        }

        static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);

        #endregion
    }
}
#endif

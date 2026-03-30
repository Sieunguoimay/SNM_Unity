#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Snm.Graphics3D.Inspection
{
    public static class FbxExporter
    {
        public static void Export(Mesh mesh, string filePath, Matrix4x4? transform = null)
        {
            var sb = new StringBuilder();
            Matrix4x4 mat = transform ?? Matrix4x4.identity;

            var verts = mesh.vertices;
            var normals = mesh.normals;
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            var tris = mesh.triangles;

            // FBX ASCII Header
            sb.AppendLine("; FBX 7.4.0 project file");
            sb.AppendLine("; Exported by Snm MeshTools");
            sb.AppendLine("FBXHeaderExtension:  {");
            sb.AppendLine("    FBXHeaderVersion: 1003");
            sb.AppendLine("    FBXVersion: 7400");
            sb.AppendLine("}");
            sb.AppendLine();

            // Global settings
            sb.AppendLine("GlobalSettings:  {");
            sb.AppendLine("    Version: 1000");
            sb.AppendLine("    Properties70:  {");
            sb.AppendLine("        P: \"UpAxis\", \"int\", \"Integer\", \"\", 1");
            sb.AppendLine("        P: \"UpAxisSign\", \"int\", \"Integer\", \"\", 1");
            sb.AppendLine("        P: \"FrontAxis\", \"int\", \"Integer\", \"\", 2");
            sb.AppendLine("        P: \"FrontAxisSign\", \"int\", \"Integer\", \"\", 1");
            sb.AppendLine("        P: \"CoordAxis\", \"int\", \"Integer\", \"\", 0");
            sb.AppendLine("        P: \"CoordAxisSign\", \"int\", \"Integer\", \"\", 1");
            sb.AppendLine("        P: \"UnitScaleFactor\", \"double\", \"Number\", \"\", 100");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();

            // Objects
            sb.AppendLine("Objects:  {");

            // Geometry
            long geoId = 100000;
            sb.AppendLine($"    Geometry: {geoId}, \"Geometry::{mesh.name}\", \"Mesh\" {{");

            // Vertices
            sb.Append("        Vertices: *").Append(verts.Length * 3).AppendLine(" {");
            sb.Append("            a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p = mat.MultiplyPoint3x4(verts[i]);
                // Negate X for right-handed conversion
                if (i > 0) sb.Append(',');
                sb.Append(F(-p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z));
            }
            sb.AppendLine();
            sb.AppendLine("        }");

            // Polygon vertex indices (FBX uses negative-1 encoding for last vertex in polygon)
            sb.Append("        PolygonVertexIndex: *").Append(tris.Length).AppendLine(" {");
            sb.Append("            a: ");
            for (int i = 0; i < tris.Length; i += 3)
            {
                // Reverse winding for right-handed
                if (i > 0) sb.Append(',');
                sb.Append(tris[i]).Append(',');
                sb.Append(tris[i + 2]).Append(',');
                sb.Append(-(tris[i + 1] + 1)); // negative-1 signals end of polygon
            }
            sb.AppendLine();
            sb.AppendLine("        }");

            // Normals
            if (normals != null && normals.Length > 0)
            {
                Matrix4x4 normalMat = mat.inverse.transpose;
                sb.AppendLine("        LayerElementNormal: 0 {");
                sb.AppendLine("            Version: 101");
                sb.AppendLine("            Name: \"\"");
                sb.AppendLine("            MappingInformationType: \"ByVertice\"");
                sb.AppendLine("            ReferenceInformationType: \"Direct\"");
                sb.Append("            Normals: *").Append(normals.Length * 3).AppendLine(" {");
                sb.Append("                a: ");
                for (int i = 0; i < normals.Length; i++)
                {
                    Vector3 n = normalMat.MultiplyVector(normals[i]).normalized;
                    if (i > 0) sb.Append(',');
                    sb.Append(F(-n.x)).Append(',').Append(F(n.y)).Append(',').Append(F(n.z));
                }
                sb.AppendLine();
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }

            // UVs
            if (uvs.Count > 0)
            {
                sb.AppendLine("        LayerElementUV: 0 {");
                sb.AppendLine("            Version: 101");
                sb.AppendLine("            Name: \"UVMap\"");
                sb.AppendLine("            MappingInformationType: \"ByVertice\"");
                sb.AppendLine("            ReferenceInformationType: \"Direct\"");
                sb.Append("            UV: *").Append(uvs.Count * 2).AppendLine(" {");
                sb.Append("                a: ");
                for (int i = 0; i < uvs.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(F(uvs[i].x)).Append(',').Append(F(uvs[i].y));
                }
                sb.AppendLine();
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }

            // Layer
            sb.AppendLine("        Layer: 0 {");
            sb.AppendLine("            Version: 100");
            if (normals != null && normals.Length > 0)
            {
                sb.AppendLine("            LayerElement:  {");
                sb.AppendLine("                Type: \"LayerElementNormal\"");
                sb.AppendLine("                TypedIndex: 0");
                sb.AppendLine("            }");
            }
            if (uvs.Count > 0)
            {
                sb.AppendLine("            LayerElement:  {");
                sb.AppendLine("                Type: \"LayerElementUV\"");
                sb.AppendLine("                TypedIndex: 0");
                sb.AppendLine("            }");
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }"); // end Geometry

            // Model node
            long modelId = 200000;
            sb.AppendLine($"    Model: {modelId}, \"Model::{mesh.name}\", \"Mesh\" {{");
            sb.AppendLine("        Version: 232");
            sb.AppendLine("        Properties70:  {");
            sb.AppendLine("            P: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\", 0, 0, 0");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            sb.AppendLine("}"); // end Objects

            // Connections
            sb.AppendLine("Connections:  {");
            sb.AppendLine($"    C: \"OO\", {modelId}, 0");
            sb.AppendLine($"    C: \"OO\", {geoId}, {modelId}");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
        }

        static string F(float v) => v.ToString("F6", CultureInfo.InvariantCulture);
    }
}
#endif

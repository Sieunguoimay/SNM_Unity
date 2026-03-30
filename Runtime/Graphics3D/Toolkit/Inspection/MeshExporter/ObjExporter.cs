#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Snm.Graphics3D.Inspection
{
    public static class ObjExporter
    {
        public static void Export(Mesh mesh, string filePath, Material[] materials = null, Matrix4x4? transform = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Exported by Snm MeshTools");
            sb.AppendLine($"# Mesh: {mesh.name}");
            sb.AppendLine($"# Vertices: {mesh.vertexCount}");
            sb.AppendLine($"# Triangles: {mesh.triangles.Length / 3}");
            sb.AppendLine();

            // MTL reference
            if (materials != null && materials.Length > 0)
            {
                string mtlFile = Path.GetFileNameWithoutExtension(filePath) + ".mtl";
                sb.AppendLine($"mtllib {mtlFile}");
                sb.AppendLine();
            }

            sb.AppendLine($"o {mesh.name}");

            var verts = mesh.vertices;
            var normals = mesh.normals;
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            Matrix4x4 mat = transform ?? Matrix4x4.identity;

            // Vertices (negate Z for right-handed conversion)
            foreach (var v in verts)
            {
                Vector3 p = mat.MultiplyPoint3x4(v);
                sb.AppendLine($"v {F(-p.x)} {F(p.y)} {F(p.z)}");
            }
            sb.AppendLine();

            // UVs
            if (uvs.Count > 0)
            {
                foreach (var uv in uvs)
                    sb.AppendLine($"vt {F(uv.x)} {F(uv.y)}");
                sb.AppendLine();
            }

            // Normals
            if (normals != null && normals.Length > 0)
            {
                Matrix4x4 normalMat = mat.inverse.transpose;
                foreach (var n in normals)
                {
                    Vector3 wn = normalMat.MultiplyVector(n).normalized;
                    sb.AppendLine($"vn {F(-wn.x)} {F(wn.y)} {F(wn.z)}");
                }
                sb.AppendLine();
            }

            // Faces per submesh
            bool hasUV = uvs.Count > 0;
            bool hasNormals = normals != null && normals.Length > 0;

            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                if (materials != null && sm < materials.Length && materials[sm] != null)
                    sb.AppendLine($"usemtl {SanitizeName(materials[sm].name)}");
                else
                    sb.AppendLine($"g submesh_{sm}");

                var desc = mesh.GetSubMesh(sm);
                var tris = mesh.triangles;

                for (int i = desc.indexStart; i < desc.indexStart + desc.indexCount; i += 3)
                {
                    // OBJ is 1-indexed, reverse winding for right-handed
                    int a = tris[i] + 1, b = tris[i + 1] + 1, c = tris[i + 2] + 1;

                    if (hasUV && hasNormals)
                        sb.AppendLine($"f {a}/{a}/{a} {c}/{c}/{c} {b}/{b}/{b}");
                    else if (hasUV)
                        sb.AppendLine($"f {a}/{a} {c}/{c} {b}/{b}");
                    else if (hasNormals)
                        sb.AppendLine($"f {a}//{a} {c}//{c} {b}//{b}");
                    else
                        sb.AppendLine($"f {a} {c} {b}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());

            // Write MTL
            if (materials != null && materials.Length > 0)
            {
                string mtlPath = Path.ChangeExtension(filePath, ".mtl");
                WriteMtl(mtlPath, materials);
            }
        }

        static void WriteMtl(string path, Material[] materials)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Exported by Snm MeshTools");

            foreach (var mat in materials)
            {
                if (mat == null) continue;
                string name = SanitizeName(mat.name);
                sb.AppendLine($"newmtl {name}");

                Color c = mat.HasProperty("_Color") ? mat.color : Color.white;
                sb.AppendLine($"Kd {F(c.r)} {F(c.g)} {F(c.b)}");
                sb.AppendLine($"d {F(c.a)}");

                if (mat.mainTexture != null)
                {
                    string texPath = UnityEditor.AssetDatabase.GetAssetPath(mat.mainTexture);
                    if (!string.IsNullOrEmpty(texPath))
                        sb.AppendLine($"map_Kd {texPath}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }

        static string F(float v) => v.ToString("F6", CultureInfo.InvariantCulture);

        static string SanitizeName(string name)
        {
            return name.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
        }
    }
}
#endif

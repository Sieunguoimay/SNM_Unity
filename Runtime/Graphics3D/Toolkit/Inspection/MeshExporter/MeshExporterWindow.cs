#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Inspection
{
    public class MeshExporterWindow : EditorWindow
    {
        enum ExportFormat { OBJ, FBX }

        [SerializeField] ExportFormat format = ExportFormat.OBJ;
        bool _applyTransform;
        bool _batchExport;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Inspect/Export Mesh...", priority = 60)]
        public static void Open()
        {
            GetWindow<MeshExporterWindow>("Mesh Exporter");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Mesh Exporter", EditorStyles.boldLabel);

            format = (ExportFormat)EditorGUILayout.EnumPopup("Format", format);
            _applyTransform = EditorGUILayout.Toggle("Apply Transform", _applyTransform);
            _batchExport = EditorGUILayout.Toggle("Batch (all selected)", _batchExport);

            EditorGUILayout.Space(4);

            // Show current selection info
            var selected = Selection.gameObjects;
            var meshes = CollectMeshes(selected);
            EditorGUILayout.LabelField("Selected Objects", selected.Length.ToString());
            EditorGUILayout.LabelField("Meshes Found", meshes.Count.ToString());

            if (meshes.Count > 0)
            {
                EditorGUILayout.Space(2);
                foreach (var (mesh, go, mats) in meshes)
                    EditorGUILayout.LabelField($"  {go.name}", $"{mesh.vertexCount}v / {mesh.triangles.Length / 3}t");
            }

            EditorGUILayout.Space(8);

            string ext = format == ExportFormat.OBJ ? "obj" : "fbx";

            EditorGUI.BeginDisabledGroup(meshes.Count == 0);

            if (!_batchExport)
            {
                if (GUILayout.Button($"Export as .{ext}", GUILayout.Height(28)))
                {
                    if (meshes.Count > 0)
                    {
                        var (mesh, go, mats) = meshes[0];
                        string path = EditorUtility.SaveFilePanel($"Export {mesh.name}", "", mesh.name, ext);
                        if (!string.IsNullOrEmpty(path))
                        {
                            ExportSingle(mesh, go, mats, path);
                            Debug.Log($"Exported: {path}");
                        }
                    }
                }
            }
            else
            {
                if (GUILayout.Button($"Batch Export as .{ext}", GUILayout.Height(28)))
                {
                    string folder = EditorUtility.SaveFolderPanel("Export Folder", "", "");
                    if (!string.IsNullOrEmpty(folder))
                    {
                        int count = 0;
                        foreach (var (mesh, go, mats) in meshes)
                        {
                            string path = System.IO.Path.Combine(folder, $"{SanitizeName(go.name)}.{ext}");
                            ExportSingle(mesh, go, mats, path);
                            count++;
                        }
                        Debug.Log($"Batch exported {count} mesh(es) to {folder}");
                    }
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Format Notes", EditorStyles.boldLabel);

            if (format == ExportFormat.OBJ)
            {
                EditorGUILayout.HelpBox(
                    "OBJ exports vertices, normals, UVs, and face indices.\n" +
                    "Submeshes are exported as material groups with a companion .mtl file.\n" +
                    "Coordinate system is converted to right-handed (Blender/Maya compatible).",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "ASCII FBX 7.4 export with vertices, normals, UVs.\n" +
                    "Importable by Blender, Maya, 3ds Max.\n" +
                    "No animation or skeleton data — geometry only.",
                    MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        void ExportSingle(Mesh mesh, GameObject go, Material[] mats, string path)
        {
            Matrix4x4? xform = _applyTransform ? go.transform.localToWorldMatrix : null;

            if (format == ExportFormat.OBJ)
                ObjExporter.Export(mesh, path, mats, xform);
            else
                FbxExporter.Export(mesh, path, xform);
        }

        static List<(Mesh mesh, GameObject go, Material[] mats)> CollectMeshes(GameObject[] gameObjects)
        {
            var result = new List<(Mesh, GameObject, Material[])>();
            var seen = new HashSet<int>();

            foreach (var go in gameObjects)
            {
                if (!seen.Add(go.GetInstanceID())) continue;

                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable)
                {
                    result.Add((mf.sharedMesh, go, mr != null ? mr.sharedMaterials : null));
                    continue;
                }

                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null && smr.sharedMesh.isReadable)
                    result.Add((smr.sharedMesh, go, smr.sharedMaterials));
            }

            return result;
        }

        static string SanitizeName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
#endif

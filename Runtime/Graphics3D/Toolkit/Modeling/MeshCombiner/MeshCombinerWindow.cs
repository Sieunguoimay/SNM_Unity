#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class MeshCombinerWindow : EditorWindow
    {
        bool _mergeByMaterial = true;
        bool _includeChildren = true;
        bool _removeSources;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Combiner", priority = 22)]
        public static void Open()
        {
            GetWindow<MeshCombinerWindow>("Mesh Combiner");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Mesh Combiner");
            EditorGUILayout.HelpBox(
                "Select multiple GameObjects with MeshFilters/SkinnedMeshRenderers, " +
                "then click Combine to merge them into a single mesh.",
                MessageType.Info);

            ToolkitGUI.SectionHeader("Options");

            _mergeByMaterial = EditorGUILayout.Toggle("Merge by Material", _mergeByMaterial);
            _includeChildren = EditorGUILayout.Toggle("Include Children", _includeChildren);
            _removeSources = EditorGUILayout.Toggle("Disable Sources After", _removeSources);

            ToolkitGUI.SectionHeader("Selection Info");

            var selected = Selection.gameObjects;
            ToolkitGUI.StatRow("Selected Objects", selected.Length.ToString());

            if (selected.Length > 0)
            {
                var inputs = MeshCombineLogic.CollectFromGameObjects(selected, _includeChildren);
                ToolkitGUI.StatRow("Meshes Found", inputs.Count.ToString());

                int totalVerts = 0, totalTris = 0;
                foreach (var input in inputs)
                {
                    totalVerts += input.Mesh.vertexCount;
                    totalTris += input.Mesh.triangles.Length / 3;
                }
                ToolkitGUI.StatRow("Total Vertices", totalVerts.ToString("N0"));
                ToolkitGUI.StatRow("Total Triangles", totalTris.ToString("N0"));

                if (totalVerts > 65535)
                    EditorGUILayout.HelpBox("Result will use 32-bit indices.", MessageType.None);
            }

            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);

            EditorGUI.BeginDisabledGroup(selected.Length == 0);

            if (ToolkitGUI.BigButton("Combine"))
                DoCombine();

            if (ToolkitGUI.ActionButton("Combine & Save as Asset"))
                DoCombineAndSave();

            EditorGUI.EndDisabledGroup();
        }

        void DoCombine()
        {
            var inputs = MeshCombineLogic.CollectFromGameObjects(Selection.gameObjects, _includeChildren);
            if (inputs.Count == 0)
            {
                Debug.LogWarning("No valid meshes found in selection.");
                return;
            }

            var result = _mergeByMaterial
                ? MeshCombineLogic.CombineByMaterial(inputs)
                : MeshCombineLogic.CombineAsSubmeshes(inputs);

            CreateResultGameObject(result);
        }

        void DoCombineAndSave()
        {
            var inputs = MeshCombineLogic.CollectFromGameObjects(Selection.gameObjects, _includeChildren);
            if (inputs.Count == 0)
            {
                Debug.LogWarning("No valid meshes found in selection.");
                return;
            }

            var result = _mergeByMaterial
                ? MeshCombineLogic.CombineByMaterial(inputs)
                : MeshCombineLogic.CombineAsSubmeshes(inputs);

            string path = EditorUtility.SaveFilePanelInProject("Save Combined Mesh", "CombinedMesh", "asset",
                "Save the combined mesh as an asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(result.CombinedMesh, path);
                AssetDatabase.SaveAssets();
            }

            CreateResultGameObject(result);
        }

        void CreateResultGameObject(MeshCombineLogic.CombineResult result)
        {
            var go = new GameObject("Combined Mesh");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = result.CombinedMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = result.Materials;

            MeshUndoHelper.RegisterCreatedGameObject(go, "Combine Meshes");

            if (_removeSources)
            {
                foreach (var selected in Selection.gameObjects)
                {
                    Undo.RecordObject(selected, "Disable Combined Source");
                    selected.SetActive(false);
                }
            }

            Selection.activeGameObject = go;
            Debug.Log($"Combined mesh: {result.CombinedMesh.vertexCount} vertices, " +
                      $"{result.CombinedMesh.triangles.Length / 3} triangles, " +
                      $"{result.Materials.Length} materials");
        }
    }
}
#endif

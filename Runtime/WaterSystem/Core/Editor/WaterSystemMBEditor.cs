#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Snm.WaterSystem.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Snm.WaterSystem
{
    [CustomEditor(typeof(WaterSystemMB))]
    public class WaterSystemMBEditor : Editor
    {
        private const string MeshOutputRoot = "Assets/Generated/WaterMeshes";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            var target = (WaterSystemMB)this.target;

            using (new EditorGUI.DisabledScope(!CanBake(target)))
            {
                if (GUILayout.Button("Bake Water Mesh", GUILayout.Height(32)))
                    Bake(target);
            }

            if (!CanBake(target))
            {
                EditorGUILayout.HelpBox(
                    "Assign at least one terrain object and set a non-zero Water Size to enable baking.",
                    MessageType.Info);
            }
        }

        private static bool CanBake(WaterSystemMB mb)
        {
            if (mb.TerrainObjects == null || mb.TerrainObjects.Count == 0) return false;
            if (mb.WaterSize.x <= 0.01f || mb.WaterSize.y <= 0.01f) return false;
            return true;
        }

        private void Bake(WaterSystemMB mb)
        {
            var terrain = new List<GameObject>(mb.TerrainObjects.Count);
            foreach (var go in mb.TerrainObjects)
                if (go != null) terrain.Add(go);

            if (terrain.Count == 0)
            {
                EditorUtility.DisplayDialog("Bake Water Mesh",
                    "Terrain list is empty (all entries are null).", "OK");
                return;
            }

            var input = new WaterMeshGenerator.Input
            {
                WaterTransform = mb.transform,
                TerrainObjects = terrain,
                WaterSize = mb.WaterSize,
                GridCellSize = mb.GridCellSize,
                MaxShoreDistance = mb.MaxShoreDistance,
                AlongShoreTiling = mb.AlongShoreTiling
            };

            var result = WaterMeshGenerator.Generate(input);
            if (result.Mesh == null || result.VertexCount == 0)
            {
                EditorUtility.DisplayDialog("Bake Water Mesh",
                    $"No mesh generated.\n\n{result.Log}", "OK");
                return;
            }

            var path = ResolveMeshAssetPath(mb);
            EnsureDirectory(path);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Mesh saved;
            if (existing != null)
            {
                EditorUtility.CopySerialized(result.Mesh, existing);
                AssetDatabase.SaveAssets();
                saved = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(result.Mesh, path);
                AssetDatabase.SaveAssets();
                saved = result.Mesh;
            }

            var so = new SerializedObject(mb);
            so.FindProperty("bakedMesh").objectReferenceValue = saved;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(mb);
            if (!EditorApplication.isPlaying)
            {
                var scene = mb.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[WaterSystemMB] Baked water mesh: {path}\n{result.Log}", saved);
        }

        private static string ResolveMeshAssetPath(WaterSystemMB mb)
        {
            string sceneName = mb.gameObject.scene.IsValid()
                ? mb.gameObject.scene.name
                : "Unsaved";
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Unsaved";

            string goName = Sanitize(mb.gameObject.name);
            string file = $"{Sanitize(sceneName)}_{goName}.asset";
            return $"{MeshOutputRoot}/{file}";
        }

        private static string Sanitize(string input)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (System.Array.IndexOf(invalid, chars[i]) >= 0 || chars[i] == ' ')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        private static void EnsureDirectory(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dir)) return;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif

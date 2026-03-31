#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class UnusedAssetFinderWindow : EditorWindow
    {
        Vector2 scroll;
        List<string> unusedPaths = new();
        string searchFolder = "Assets";
        bool includeScripts = false;
        bool analysisReady = false;

        [MenuItem("Tools/Snm/YAML Index/Find Unused Assets")]
        public static void Open()
        {
            GetWindow<UnusedAssetFinderWindow>("Unused Assets");
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            searchFolder = EditorGUILayout.TextField(
                "Search Folder", searchFolder);
            includeScripts = EditorGUILayout.Toggle(
                "Include Scripts (.cs)", includeScripts);

            EditorGUILayout.Space();

            if (GUILayout.Button("Find Unused Assets",
                GUILayout.Height(30)))
            {
                FindUnused();
            }

            if (analysisReady)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    $"Unused Assets: {unusedPaths.Count}",
                    EditorStyles.boldLabel);

                DrawResults();
            }
        }

        void FindUnused()
        {
            EditorUtility.DisplayProgressBar(
                "Unused Assets", "Building reference map...", 0f);

            try
            {
                var reverseMap =
                    YamlIndexQuery.BuildReverseReferenceMap();

                // Collect all GUIDs referenced by any indexed asset
                var referencedGuids = new HashSet<string>(
                    reverseMap.Keys);

                // Also add GUIDs that appear in scene build settings
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    var g = AssetDatabase.AssetPathToGUID(scene.path);
                    if (!string.IsNullOrEmpty(g))
                        referencedGuids.Add(g);
                }

                // Find all assets under searchFolder
                var allGuids = AssetDatabase.FindAssets(
                    "", new[] { searchFolder });

                unusedPaths.Clear();

                for (int i = 0; i < allGuids.Length; i++)
                {
                    if (i % 500 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Unused Assets",
                            $"Checking {i}/{allGuids.Length}...",
                            (float)i / allGuids.Length);
                    }

                    var guid = allGuids[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip folders
                    if (AssetDatabase.IsValidFolder(path))
                        continue;

                    // Skip scripts unless opted in
                    if (!includeScripts && YamlIndexQuery.IsScript(path))
                        continue;

                    // Skip .meta files and special paths
                    if (path.EndsWith(".meta"))
                        continue;

                    // An asset is "unused" if no other indexed asset
                    // references its GUID
                    if (!referencedGuids.Contains(guid))
                        unusedPaths.Add(path);
                }

                unusedPaths.Sort();
                analysisReady = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void DrawResults()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var path in unusedPaths)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button("Select", GUILayout.Width(55)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    Selection.activeObject = obj;
                }

                EditorGUILayout.LabelField(path);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

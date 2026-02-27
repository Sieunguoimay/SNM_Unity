#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public class AssetReferenceFinderWindow : EditorWindow
    {
        public List<Object> searchRoots = new();
        public List<Object> targetRoots = new();

        private Vector2 scroll;
        private List<string> results = new();

        private bool flipSearch = false;

        [MenuItem("Tools/Snm/Asset Reference Finder")]
        public static void Open()
        {
            GetWindow<AssetReferenceFinderWindow>("Asset Reference Finder");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            DrawObjectList("Search In (assets / folders)", searchRoots);
            EditorGUILayout.Space();
            DrawObjectList("Target (assets / folders)", targetRoots);

            EditorGUILayout.Space();
            flipSearch = EditorGUILayout.ToggleLeft(
                "Flip Search (find references FROM Target to OUTSIDE Target)",
                flipSearch);

            EditorGUILayout.Space();

            if (GUILayout.Button("Find References", GUILayout.Height(30)))
            {
                FindReferences();
            }

            EditorGUILayout.Space();
            DrawResults();
        }

        #region UI Helpers

        private void DrawObjectList(string label, List<UnityEngine.Object> list)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.ObjectField(list[i], typeof(UnityEngine.Object), false);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
                list.RemoveAt(removeIndex);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected"))
            {
                foreach (var obj in Selection.objects)
                {
                    if (!list.Contains(obj))
                        list.Add(obj);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                list.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResults()
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var path in results)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.LabelField(path);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Core Logic

        public void FindReferences()
        {
            results.Clear();

            var searchGuids = CollectGuids(searchRoots);
            var targetGuids = CollectGuids(targetRoots);

            if (!flipSearch)
            {
                // Find references to Target inside Search
                foreach (var guid in searchGuids)
                {
                    var deps = AssetDatabase.GetDependencies(
                        AssetDatabase.GUIDToAssetPath(guid),
                        true);

                    foreach (var dep in deps)
                    {
                        var depGuid = AssetDatabase.AssetPathToGUID(dep);

                        if (targetGuids.Contains(depGuid))
                        {
                            results.Add(AssetDatabase.GUIDToAssetPath(guid));
                            break;
                        }
                    }
                }
            }
            else
            {
                // Find references FROM Target to OUTSIDE Target
                foreach (var guid in targetGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    var deps = AssetDatabase.GetDependencies(path, true);

                    foreach (var dep in deps)
                    {
                        var depGuid = AssetDatabase.AssetPathToGUID(dep);

                        if (!targetGuids.Contains(depGuid))
                        {
                            results.Add(path);
                            break;
                        }
                    }
                }
            }

            results = results.Distinct().OrderBy(p => p).ToList();
        }

        private HashSet<string> CollectGuids(List<UnityEngine.Object> roots)
        {
            var guids = new HashSet<string>();

            foreach (var obj in roots)
            {
                if (obj == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(path))
                    continue;

                if (AssetDatabase.IsValidFolder(path))
                {
                    var folderGuids = AssetDatabase.FindAssets("", new[] { path });
                    foreach (var g in folderGuids)
                        guids.Add(g);
                }
                else
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    guids.Add(guid);
                }
            }

            return guids;
        }

        #endregion
    }
}

#endif
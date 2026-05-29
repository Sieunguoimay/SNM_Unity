#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Snm.Tools.Finders
{
    public class ReferenceFinderWindow : EditorWindow
    {
        public enum SearchDomain { SceneOrPrefab, ProjectAssets }

        public SearchDomain domain = SearchDomain.SceneOrPrefab;
        public List<Object> targets = new();
        public List<Object> projectSearchRoots = new();
        public bool flipSearch;

        [SerializeField] private List<Result> results = new();
        [SerializeField] private string resultSearch = "";
        private Vector2 scroll;

        [MenuItem("Tools/Snm/Finders/Find References")]
        public static void Open()
        {
            GetWindow<ReferenceFinderWindow>("Reference Finder");
        }

        public static void OpenForScene(Object target)
        {
            var w = GetWindow<ReferenceFinderWindow>("Reference Finder");
            w.domain = SearchDomain.SceneOrPrefab;
            w.targets.Clear();
            if (target != null) w.targets.Add(target);
            w.Find();
            w.Show();
        }

        public static void OpenForProject(Object target)
        {
            var w = GetWindow<ReferenceFinderWindow>("Reference Finder");
            w.domain = SearchDomain.ProjectAssets;
            w.targets.Clear();
            if (target != null) w.targets.Add(target);
            if (w.projectSearchRoots.Count == 0)
                w.projectSearchRoots.Add(AssetDatabase.LoadAssetAtPath<Object>("Assets"));
            w.Find();
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            domain = (SearchDomain)EditorGUILayout.EnumPopup("Domain", domain);
            EditorGUILayout.Space();

            var so = new SerializedObject(this);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(targets)));

            if (domain == SearchDomain.ProjectAssets)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(so.FindProperty(nameof(projectSearchRoots)));
                EditorGUILayout.Space();

                flipSearch = EditorGUILayout.ToggleLeft(
                    "Flip Search (find references FROM Target to OUTSIDE Target)",
                    flipSearch);
            }
            so.ApplyModifiedProperties();

            EditorGUILayout.Space();

            GUI.enabled = targets.Any(t => t != null);
            if (GUILayout.Button(FindButtonLabel(), GUILayout.Height(30))) Find();
            GUI.enabled = true;

            EditorGUILayout.Space();
            DrawResults();
        }

        private string FindButtonLabel()
        {
            if (domain == SearchDomain.ProjectAssets) return "Find References";
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null
                ? $"Find References In Prefab ({prefabStage.prefabContentsRoot.name})"
                : $"Find References In Scene ({SceneManager.GetActiveScene().name})";
        }

        public void Find()
        {
            results.Clear();
            var nonNullTargets = targets.Where(t => t != null).ToList();
            if (nonNullTargets.Count == 0) return;

            if (domain == SearchDomain.SceneOrPrefab)
                FindInSceneOrPrefab(nonNullTargets);
            else
                FindInProject(nonNullTargets);
        }

        // ── Scene/Prefab ────────────────────────────────────────

        private void FindInSceneOrPrefab(List<Object> targetSet)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var roots = prefabStage != null
                ? prefabStage.scene.GetRootGameObjects()
                : SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots) TraverseGameObject(root, targetSet);
        }

        private void TraverseGameObject(GameObject go, List<Object> targetSet)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue == null) continue;
                    if (!targetSet.Contains(prop.objectReferenceValue)) continue;
                    results.Add(new Result
                    {
                        Kind = ResultKind.SceneReference,
                        GameObjectPath = GetHierarchyPath(go),
                        Component = comp,
                        PropertyPath = prop.propertyPath
                    });
                }
            }
            foreach (Transform child in go.transform)
                TraverseGameObject(child.gameObject, targetSet);
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        // ── Project Assets ──────────────────────────────────────

        private void FindInProject(List<Object> targetSet)
        {
            var searchGuids = CollectGuids(projectSearchRoots);
            var targetGuids = CollectGuids(targetSet);

            var hits = new HashSet<string>();
            if (!flipSearch)
            {
                foreach (var guid in searchGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var dep in AssetDatabase.GetDependencies(path, true))
                    {
                        if (targetGuids.Contains(AssetDatabase.AssetPathToGUID(dep)))
                        {
                            hits.Add(path);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var guid in targetGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var dep in AssetDatabase.GetDependencies(path, true))
                    {
                        if (!targetGuids.Contains(AssetDatabase.AssetPathToGUID(dep)))
                        {
                            hits.Add(path);
                            break;
                        }
                    }
                }
            }

            foreach (var path in hits.OrderBy(p => p))
                results.Add(new Result { Kind = ResultKind.AssetPath, AssetPath = path });
        }

        private static HashSet<string> CollectGuids(List<Object> roots)
        {
            var guids = new HashSet<string>();
            foreach (var obj in roots)
            {
                if (obj == null) continue;
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path))
                    foreach (var g in AssetDatabase.FindAssets("", new[] { path })) guids.Add(g);
                else
                    guids.Add(AssetDatabase.AssetPathToGUID(path));
            }
            return guids;
        }

        // ── Results UI ──────────────────────────────────────────

        private void DrawResults()
        {
            var filtered = results.Where(r => MatchesSearch(r, resultSearch)).ToList();

            EditorGUILayout.LabelField(
                $"Results ({filtered.Count}/{results.Count})",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            resultSearch = EditorGUILayout.TextField(resultSearch);
            GUILayout.Label(
                EditorGUIUtility.IconContent("Search Icon"),
                GUILayout.Width(18), GUILayout.Height(16));
            if (!string.IsNullOrEmpty(resultSearch) &&
                GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
                resultSearch = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var r in filtered)
            {
                if (r.Kind == ResultKind.SceneReference) DrawSceneCard(r);
                else DrawAssetCard(r);
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSceneCard(Result r)
        {
            if (r.Component == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                new GUIContent(r.GameObjectPath, r.GameObjectPath),
                EditorStyles.boldLabel);
            if (GUILayout.Button(
                    new GUIContent("Ping", "Highlight in Hierarchy"),
                    EditorStyles.miniButton, GUILayout.Width(40)))
                EditorGUIUtility.PingObject(r.Component);
            if (GUILayout.Button(
                    new GUIContent("Select", "Select component"),
                    EditorStyles.miniButton, GUILayout.Width(50)))
            {
                Selection.activeObject = r.Component;
                EditorGUIUtility.PingObject(r.Component);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Component", r.Component.GetType().Name);
            EditorGUILayout.LabelField("Property", r.PropertyPath);
            EditorGUILayout.EndVertical();
        }

        private static void DrawAssetCard(Result r)
        {
            var path = r.AssetPath;
            var fileName = System.IO.Path.GetFileName(path);
            var folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var icon = AssetDatabase.GetCachedIcon(path);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            var typeName = assetType != null ? assetType.Name : "";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (icon != null) GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(
                new GUIContent(fileName, path),
                EditorStyles.boldLabel);
            if (GUILayout.Button(
                    new GUIContent("Ping", "Highlight in Project"),
                    EditorStyles.miniButton, GUILayout.Width(40)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            if (GUILayout.Button(
                    new GUIContent("Select", "Select asset"),
                    EditorStyles.miniButton, GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Folder", folder);
            if (!string.IsNullOrEmpty(typeName))
                EditorGUILayout.LabelField("Type", typeName);
            EditorGUILayout.EndVertical();
        }

        private static bool MatchesSearch(Result r, string q)
        {
            if (string.IsNullOrEmpty(q)) return true;
            if (r.Kind == ResultKind.SceneReference)
            {
                if (r.Component == null) return false;
                var typeName = r.Component.GetType().Name;
                return (r.GameObjectPath != null &&
                        r.GameObjectPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    || typeName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || (r.PropertyPath != null &&
                        r.PropertyPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return r.AssetPath != null &&
                   r.AssetPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private enum ResultKind { SceneReference, AssetPath }

        [Serializable]
        private class Result
        {
            public ResultKind Kind;
            public string GameObjectPath;
            public Component Component;
            public string PropertyPath;
            public string AssetPath;
        }
    }
}
#endif

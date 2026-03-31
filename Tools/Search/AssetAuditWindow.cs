#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class AssetAuditWindow : EditorWindow
    {
        enum AuditMode
        {
            PrefabsUsingScript,
            ScenesContainingPrefab,
            PrefabInstanceCounts,
        }

        AuditMode mode = AuditMode.PrefabsUsingScript;
        Object targetAsset;
        Vector2 scroll;
        List<AuditResult> results = new();
        bool analysisReady;

        struct AuditResult
        {
            public string path;
            public int count;
        }

        [MenuItem("Tools/Snm/YAML Index/Asset Audit")]
        public static void Open()
        {
            GetWindow<AssetAuditWindow>("Asset Audit");
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            mode = (AuditMode)EditorGUILayout.EnumPopup("Mode", mode);

            string label = mode switch
            {
                AuditMode.PrefabsUsingScript =>
                    "Script (.cs)",
                AuditMode.ScenesContainingPrefab =>
                    "Prefab",
                AuditMode.PrefabInstanceCounts =>
                    "Prefab",
                _ => "Asset"
            };

            targetAsset = EditorGUILayout.ObjectField(
                label, targetAsset, typeof(Object), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Audit", GUILayout.Height(30)))
            {
                RunAudit();
            }

            if (analysisReady)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    $"Results: {results.Count}",
                    EditorStyles.boldLabel);
                DrawResults();
            }
        }

        void RunAudit()
        {
            results.Clear();
            analysisReady = false;

            if (targetAsset == null)
                return;

            var targetPath = AssetDatabase.GetAssetPath(targetAsset);
            var targetGuid = AssetDatabase.AssetPathToGUID(targetPath);

            EditorUtility.DisplayProgressBar(
                "Asset Audit", "Scanning index...", 0f);

            try
            {
                YamlIndexDatabase.EnsureIndexReady();

                switch (mode)
                {
                    case AuditMode.PrefabsUsingScript:
                        FindAssetsReferencing(
                            targetGuid, ".prefab");
                        break;

                    case AuditMode.ScenesContainingPrefab:
                        FindAssetsReferencing(
                            targetGuid, ".unity");
                        break;

                    case AuditMode.PrefabInstanceCounts:
                        CountReferencesAcrossScenes(targetGuid);
                        break;
                }

                analysisReady = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void FindAssetsReferencing(
            string targetGuid, string extension)
        {
            var entries = YamlIndexDatabase.Entries;
            int i = 0;

            foreach (var entry in entries)
            {
                if (i++ % 200 == 0)
                    EditorUtility.DisplayProgressBar(
                        "Asset Audit",
                        $"Scanning {i}/{entries.Count}...",
                        (float)i / entries.Count);

                if (!entry.path.EndsWith(extension))
                    continue;

                var refs = YamlIndexQuery
                    .ExtractReferencedGuids(entry);

                if (refs.Contains(targetGuid))
                {
                    results.Add(new AuditResult
                    {
                        path = entry.path,
                        count = 1
                    });
                }
            }

            results = results
                .OrderBy(r => r.path)
                .ToList();
        }

        void CountReferencesAcrossScenes(string targetGuid)
        {
            var entries = YamlIndexDatabase.Entries;
            int i = 0;
            string pattern = $"guid: {targetGuid}";

            foreach (var entry in entries)
            {
                if (i++ % 200 == 0)
                    EditorUtility.DisplayProgressBar(
                        "Asset Audit",
                        $"Counting in {i}/{entries.Count}...",
                        (float)i / entries.Count);

                if (!entry.path.EndsWith(".unity") &&
                    !entry.path.EndsWith(".prefab"))
                    continue;

                if (entry.lines == null)
                    continue;

                int count = 0;
                foreach (var line in entry.lines)
                {
                    if (line.Contains(pattern))
                        count++;
                }

                if (count > 0)
                {
                    results.Add(new AuditResult
                    {
                        path = entry.path,
                        count = count
                    });
                }
            }

            results = results
                .OrderByDescending(r => r.count)
                .ToList();
        }

        void DrawResults()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            bool showCount =
                mode == AuditMode.PrefabInstanceCounts;

            foreach (var r in results)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(
                        r.path);
                    EditorGUIUtility.PingObject(obj);
                }

                if (showCount)
                {
                    EditorGUILayout.LabelField(
                        r.count.ToString(),
                        GUILayout.Width(35));
                }

                EditorGUILayout.LabelField(r.path);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

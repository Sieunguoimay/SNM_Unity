#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class ScriptUsageWindow : EditorWindow
    {
        enum ViewMode { AllScripts, SingleScript }

        ViewMode viewMode = ViewMode.AllScripts;
        Object targetScript;
        string filterFolder = "Assets/Scripts";
        Vector2 scroll;
        bool analysisReady;

        // AllScripts mode
        List<ScriptReport> reports = new();

        // SingleScript mode
        List<string> usagePaths = new();

        struct ScriptReport
        {
            public string scriptPath;
            public string scriptGuid;
            public int usageCount;
        }

        [MenuItem("Tools/Snm/YAML Index/Script Usage Report")]
        public static void Open()
        {
            GetWindow<ScriptUsageWindow>("Script Usage");
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            viewMode = (ViewMode)EditorGUILayout.EnumPopup(
                "View", viewMode);

            if (viewMode == ViewMode.AllScripts)
            {
                filterFolder = EditorGUILayout.TextField(
                    "Scripts Folder", filterFolder);
            }
            else
            {
                targetScript = EditorGUILayout.ObjectField(
                    "Script", targetScript,
                    typeof(MonoScript), false);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Analyze", GUILayout.Height(30)))
            {
                if (viewMode == ViewMode.AllScripts)
                    AnalyzeAll();
                else
                    AnalyzeSingle();
            }

            if (analysisReady)
            {
                EditorGUILayout.Space();

                if (viewMode == ViewMode.AllScripts)
                    DrawAllScriptsResults();
                else
                    DrawSingleScriptResults();
            }
        }

        void AnalyzeAll()
        {
            reports.Clear();
            analysisReady = false;

            EditorUtility.DisplayProgressBar(
                "Script Usage", "Building reference map...", 0f);

            try
            {
                var reverseMap =
                    YamlIndexQuery.BuildReverseReferenceMap();

                var scriptGuids = AssetDatabase.FindAssets(
                    "t:MonoScript",
                    new[] { filterFolder });

                for (int i = 0; i < scriptGuids.Length; i++)
                {
                    if (i % 100 == 0)
                        EditorUtility.DisplayProgressBar(
                            "Script Usage",
                            $"Checking {i}/{scriptGuids.Length}...",
                            (float)i / scriptGuids.Length);

                    var guid = scriptGuids[i];
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    int count = 0;
                    if (reverseMap.TryGetValue(guid, out var refs))
                        count = refs.Count;

                    reports.Add(new ScriptReport
                    {
                        scriptPath = path,
                        scriptGuid = guid,
                        usageCount = count
                    });
                }

                reports = reports
                    .OrderBy(r => r.usageCount)
                    .ThenBy(r => r.scriptPath)
                    .ToList();

                analysisReady = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void AnalyzeSingle()
        {
            usagePaths.Clear();
            analysisReady = false;

            if (targetScript == null)
                return;

            var scriptPath =
                AssetDatabase.GetAssetPath(targetScript);
            var scriptGuid =
                AssetDatabase.AssetPathToGUID(scriptPath);

            EditorUtility.DisplayProgressBar(
                "Script Usage", "Scanning index...", 0f);

            try
            {
                YamlIndexDatabase.EnsureIndexReady();

                var entries = YamlIndexDatabase.Entries;
                int i = 0;

                foreach (var entry in entries)
                {
                    if (i++ % 200 == 0)
                        EditorUtility.DisplayProgressBar(
                            "Script Usage",
                            $"Scanning {i}/{entries.Count}...",
                            (float)i / entries.Count);

                    var refs = YamlIndexQuery
                        .ExtractReferencedGuids(entry);

                    if (refs.Contains(scriptGuid))
                        usagePaths.Add(entry.path);
                }

                usagePaths.Sort();
                analysisReady = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void DrawAllScriptsResults()
        {
            int unused = reports.Count(r => r.usageCount == 0);

            EditorGUILayout.LabelField(
                $"Scripts: {reports.Count}  |  " +
                $"Unused: {unused}  |  " +
                $"Used: {reports.Count - unused}",
                EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var r in reports)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(
                        r.scriptPath);
                    EditorGUIUtility.PingObject(obj);
                }

                var style = r.usageCount == 0
                    ? new GUIStyle(EditorStyles.label)
                      { normal = { textColor = Color.red } }
                    : EditorStyles.label;

                EditorGUILayout.LabelField(
                    r.usageCount.ToString(),
                    GUILayout.Width(35));

                EditorGUILayout.LabelField(r.scriptPath, style);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawSingleScriptResults()
        {
            EditorGUILayout.LabelField(
                $"Referenced by {usagePaths.Count} assets",
                EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var path in usagePaths)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(
                        path);
                    EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.LabelField(path);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

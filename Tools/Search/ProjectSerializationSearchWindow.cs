#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Snm.Tools.Finders
{
    public class ProjectSerializationSearchWindow : EditorWindow
    {
        enum Mode
        {
            String,
            FieldName,
            MissingReferences,
            EmptyString
        }

        const int MaxResults = 1000;
        const int ProgressUpdateEvery = 50;

        // Built-in resources and the null reference both have GUIDs whose first
        // 16 hex chars are zero. Real project assets are 128-bit random and
        // effectively never collide with this prefix.
        const string BuiltInOrNullGuidPrefix = "0000000000000000";

        [SerializeField] string searchText;
        [SerializeField] bool regex;
        [SerializeField] bool caseSensitive;
        [SerializeField] UnityEngine.Object[] targets;
        [SerializeField] Mode _mode;

        Vector2 _scroll;
        bool truncated;

        struct Result
        {
            public string path;
            public int lineNumber;
            public string preview;
        }

        readonly List<Result> results = new();

        [MenuItem("Tools/Snm/Finders/Serialization Search")]
        static void Open()
        {
            var window = GetWindow<ProjectSerializationSearchWindow>();
            window.titleContent = new GUIContent("Serialization Search");
            window.Show();
        }

        void OnEnable()
        {
            // Seed scope only when nothing is persisted, so reopening the window
            // doesn't trample whatever the user last selected.
            if (targets == null || targets.Length == 0)
            {
                targets = new[]
                {
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets")
                };
            }
        }

        void OnGUI()
        {
            _mode = (Mode)GUILayout.Toolbar(
                (int)_mode,
                Enum.GetNames(typeof(Mode)));

            EditorGUILayout.Space();

            DrawScope();

            if (ModeNeedsSearchText())
            {
                regex =
                    EditorGUILayout.Toggle("Regex", regex);

                caseSensitive =
                    EditorGUILayout.Toggle("Case Sensitive", caseSensitive);

                searchText =
                    EditorGUILayout.TextField("Search", searchText);
            }

            bool needsText = ModeNeedsSearchText();
            bool canSearch = !needsText || !string.IsNullOrEmpty(searchText);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!canSearch))
            {
                if (GUILayout.Button("Search"))
                    ExecuteSearch();
            }
            if (GUILayout.Button("Rebuild Index", GUILayout.Width(120)))
            {
                YamlIndexerBuilder.Rebuild();
            }
            EditorGUILayout.EndHorizontal();

            if (needsText && string.IsNullOrEmpty(searchText))
            {
                EditorGUILayout.HelpBox(
                    "Enter search text to run.",
                    MessageType.Info);
            }

            DrawResults();
        }

        void DrawScope()
        {
            SerializedObject so =
                new SerializedObject(this);

            EditorGUILayout.PropertyField(
                so.FindProperty(nameof(targets)),
                true);

            so.ApplyModifiedProperties();
        }

        // =======================

        void ExecuteSearch()
        {
            results.Clear();
            truncated = false;

            YamlIndexDatabase.EnsureIndexReady();

            var matcher = BuildLineMatcher();
            if (matcher == null)
                return;

            BuildScope(out var fileScope, out var folderScope);
            bool hasScope = fileScope.Count > 0 || folderScope.Count > 0;

            // Snapshot so the count is stable for the progress bar even if the
            // index mutates underneath us mid-scan.
            var entries = new List<YamlAssetIndex.Entry>(YamlIndexDatabase.Entries);
            int total = entries.Count;
            bool cancelled = false;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    if (i % ProgressUpdateEvery == 0)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(
                            "Serialization Search",
                            $"Scanning {i}/{total}",
                            i / (float)total))
                        {
                            cancelled = true;
                            break;
                        }
                    }

                    var entry = entries[i];
                    if (hasScope && !InScope(entry.path, fileScope, folderScope))
                        continue;

                    for (int lineIdx = 0; lineIdx < entry.lines.Length; lineIdx++)
                    {
                        var line = entry.lines[lineIdx];
                        if (!matcher(line)) continue;

                        results.Add(new Result
                        {
                            path = entry.path,
                            lineNumber = lineIdx + 1,
                            preview = line.Trim()
                        });

                        if (results.Count >= MaxResults)
                        {
                            truncated = true;
                            break;
                        }
                    }

                    if (truncated) break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (cancelled)
            {
                Debug.Log($"Serialization Search cancelled after scanning entries; partial results: {results.Count}");
            }
        }

        bool ModeNeedsSearchText()
        {
            return _mode switch
            {
                Mode.String => true,
                Mode.FieldName => true,
                _ => false
            };
        }

        Func<string, bool> BuildLineMatcher()
        {
            switch (_mode)
            {
                case Mode.String:
                    return BuildTextMatcher(searchText);

                case Mode.FieldName:
                    return BuildTextMatcher(searchText + ":");

                case Mode.EmptyString:
                    var emptyRegex = new Regex(@":\s*""""", RegexOptions.Compiled);
                    return emptyRegex.IsMatch;

                case Mode.MissingReferences:
                    var guidRegex = new Regex(@"guid:\s*([a-fA-F0-9]{32})", RegexOptions.Compiled);
                    return line =>
                    {
                        var m = guidRegex.Match(line);
                        if (!m.Success) return false;

                        var guid = m.Groups[1].Value;

                        // Skip Unity's null sentinel and built-in resource GUIDs —
                        // they legitimately resolve to empty paths and would otherwise
                        // flood results with false positives.
                        if (guid.StartsWith(BuiltInOrNullGuidPrefix, StringComparison.Ordinal))
                            return false;

                        return string.IsNullOrEmpty(
                            AssetDatabase.GUIDToAssetPath(guid));
                    };
            }

            return null;
        }

        Func<string, bool> BuildTextMatcher(string needle)
        {
            if (string.IsNullOrEmpty(needle))
                return _ => false;

            if (regex)
            {
                var r = new Regex(
                    needle,
                    RegexOptions.Compiled |
                    (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase));

                return r.IsMatch;
            }

            if (caseSensitive)
                return line => line.Contains(needle);

            return line => line.IndexOf(
                needle,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void BuildScope(out HashSet<string> files, out List<string> folders)
        {
            files = new HashSet<string>();
            folders = new List<string>();

            if (targets == null)
                return;

            foreach (var obj in targets)
            {
                if (obj == null) continue;

                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                if (Directory.Exists(path))
                    folders.Add(path);
                else
                    files.Add(path);
            }
        }

        static bool InScope(string entryPath, HashSet<string> files, List<string> folders)
        {
            if (files.Contains(entryPath))
                return true;

            foreach (var folder in folders)
            {
                if (entryPath == folder)
                    return true;

                if (entryPath.StartsWith(folder + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        void DrawResults()
        {
            var header = truncated
                ? $"Results ({results.Count}, truncated — narrow your search)"
                : $"Results ({results.Count})";

            GUILayout.Label(header, EditorStyles.boldLabel);

            _scroll =
                EditorGUILayout.BeginScrollView(_scroll);

            foreach (var r in results)
            {
                if (GUILayout.Button(
                    $"{r.path}:{r.lineNumber}   ▶   {r.preview}",
                    EditorStyles.linkLabel))
                {
                    var obj =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(r.path);

                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}

#endif

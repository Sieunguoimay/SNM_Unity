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


        [SerializeField] string searchText;
        [SerializeField] bool regex;
        [SerializeField] bool caseSensitive;
        [SerializeField] UnityEngine.Object[] targets;

        Mode _mode;
        Vector2 _scroll;

        struct Result
        {
            public string path;
            public string preview;
        }

        readonly List<Result> results = new();

        [MenuItem("Tools/Snm/Finders/Serialization Search")]
        static void Open()
        {
            var window = GetWindow<ProjectSerializationSearchWindow>();
            window.titleContent = new GUIContent("Serialization Search");
            window.targets = new[] { AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets") };
            window.Show();
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
            if (GUILayout.Button("Search"))
                ExecuteSearch();

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

            YamlIndexDatabase.EnsureIndexReady();

            var matcher = BuildLineMatcher();
            if (matcher == null)
                return;

            BuildScope(out var fileScope, out var folderScope);
            bool hasScope = fileScope.Count > 0 || folderScope.Count > 0;

            foreach (var entry in YamlIndexDatabase.Entries)
            {
                if (hasScope && !InScope(entry.path, fileScope, folderScope))
                    continue;

                foreach (var line in entry.lines)
                {
                    if (matcher(line))
                    {
                        results.Add(new Result
                        {
                            path = entry.path,
                            preview = line.Trim()
                        });
                    }
                }
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
                        return string.IsNullOrEmpty(
                            AssetDatabase.GUIDToAssetPath(m.Groups[1].Value));
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
            GUILayout.Label(
                $"Results ({results.Count})",
                EditorStyles.boldLabel);

            _scroll =
                EditorGUILayout.BeginScrollView(_scroll);

            foreach (var r in results)
            {
                if (GUILayout.Button(
                    $"{r.path}   ▶   {r.preview}",
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
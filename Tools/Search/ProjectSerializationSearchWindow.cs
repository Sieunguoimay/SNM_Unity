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

            foreach (var entry in YamlIndexDatabase.Entries)
            {
                foreach (var line in entry.lines)
                {
                    if (MatchLine(line))
                    {
                        results.Add(new Result
                        {
                            path = entry.path,
                            preview = line.Trim()
                        });
                    }
                }
            }
            // foreach (var path in GetScopePaths())
            // {
            //     var full =
            //         Path.Combine(Application.dataPath,
            //         path["Assets/".Length..]);

            //     if (!File.Exists(full))
            //         continue;

            //     var lines = File.ReadAllLines(full);

            //     for (int i = 0; i < lines.Length; i++)
            //     {
            //         if (MatchLine(lines[i]))
            //         {
            //             results.Add(new Result
            //             {
            //                 path = path,
            //                 preview = lines[i].Trim()
            //             });
            //         }
            //     }
            // }
        }

        bool MatchLine(string line)
        {
            switch (_mode)
            {
                case Mode.String:
                    return Match(line, searchText);

                case Mode.FieldName:
                    return Match(line, searchText + ":");

                case Mode.EmptyString:
                    return Regex.IsMatch(line, @":\s*""""");

                case Mode.MissingReferences:
                    return IsMissingReference(line);
            }

            return false;
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

        bool Match(string source, string value)
        {
            if (regex)
            {
                var r = new Regex(
                    value,
                    caseSensitive
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase);

                return r.IsMatch(source);
            }

            return caseSensitive
                ? source.Contains(value)
                : source.IndexOf(
                    value,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool IsMissingReference(string line)
        {
            var match =
                Regex.Match(line,
                @"guid:\s*([a-fA-F0-9]{32})");

            if (!match.Success)
                return false;

            var guid = match.Groups[1].Value;

            return string.IsNullOrEmpty(
                AssetDatabase.GUIDToAssetPath(guid));
        }

        IEnumerable<string> GetScopePaths()
        {
            if (targets == null ||
                targets.Length == 0)
            {
                foreach (var guid in AssetDatabase.FindAssets(""))
                {
                    yield return AssetDatabase.GUIDToAssetPath(guid);
                }
            }

            foreach (var obj in targets)
            {
                var path =
                    AssetDatabase.GetAssetPath(obj);

                if (Directory.Exists(path))
                {
                    foreach (var g in
                        AssetDatabase.FindAssets("", new[] { path }))
                    {
                        yield return
                            AssetDatabase.GUIDToAssetPath(g);
                    }
                }
                else
                    yield return path;
            }
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
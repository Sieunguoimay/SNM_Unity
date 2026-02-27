#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Snm.Tools
{
    public class RenameToolWindow : EditorWindow
    {
        [SerializeField] Object[] targets = Array.Empty<Object>();
        [SerializeField] string prefix = "";
        [SerializeField] string suffix = "";
        [SerializeField] string find = "";
        [SerializeField] string replace = "";
        [SerializeField] bool useRegex;
        [SerializeField] string regexPattern = "";
        [SerializeField] string regexReplace = "";
        [SerializeField] bool numbering;
        [SerializeField] int numberStart = 1;
        [SerializeField] int padding = 3;
        [SerializeField] CaseMode caseMode;

        Vector2 _scroll;
        string[] _previewNames = Array.Empty<string>();

        enum CaseMode { None, Upper, Lower, Title }

        // ======================================================
        // OPEN API
        // ======================================================

        public static void Open(params Object[] targets)
        {
            var window = GetWindow<RenameToolWindow>();
            window.titleContent = new GUIContent("Rename Tool");

            window.SetTargets(targets);
            window.Show();
        }

        [MenuItem("Tools/Snm/Edit/Rename Tool")]
        static void OpenFromMenu()
        {
            Open(Selection.objects);
        }

        // ======================================================

        void SetTargets(Object[] objs)
        {
            targets = objs
                .Where(o => o != null)
                .Distinct()
                .ToArray();

            GeneratePreview();
        }

        void OnSelectionChange()
        {
            if (targets.Length == 0)
                SetTargets(Selection.objects);
        }

        // ======================================================

        void OnGUI()
        {
            var serializedObject = new SerializedObject(this);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(targets)), new GUIContent("Targets"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                GeneratePreview();
            }

            prefix = EditorGUILayout.TextField("Prefix", prefix);
            suffix = EditorGUILayout.TextField("Suffix", suffix);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            find = EditorGUILayout.TextField("Replace", find);
            GUILayout.Label("->", GUILayout.Width(20));
            replace = EditorGUILayout.TextField(replace);
            EditorGUILayout.EndHorizontal();

            useRegex =
                EditorGUILayout.Toggle("Use Regex", useRegex);

            if (useRegex)
            {
                regexPattern =
                    EditorGUILayout.TextField("Pattern", regexPattern);

                regexReplace =
                    EditorGUILayout.TextField("Replace", regexReplace);
            }

            numbering =
                EditorGUILayout.Toggle("Numbering", numbering);

            if (numbering)
            {
                numberStart =
                    EditorGUILayout.IntField("Start", numberStart);

                padding =
                    EditorGUILayout.IntField("Padding", padding);
            }

            caseMode =
                (CaseMode)EditorGUILayout.EnumPopup(
                    "Case",
                    caseMode);

            if (GUILayout.Button("Preview"))
                GeneratePreview();

            DrawPreview();

            GUI.enabled = targets.Length > 0;

            if (GUILayout.Button("Apply Rename", GUILayout.Height(28)))
                ApplyRename();

            GUI.enabled = true;
        }

        // ======================================================

        void DrawPreview()
        {
            _scroll = EditorGUILayout.BeginScrollView(
                _scroll,
                GUILayout.Height(220));

            for (int i = 0; i < targets.Length; i++)
            {
                _previewNames[i] = EditorGUILayout.TextField(
                    targets[i].name + " -> ",
                    _previewNames[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        // ======================================================

        void GeneratePreview()
        {
            _previewNames = targets
                .Select((t, i) => Rename(t.name, i))
                .ToArray();
        }

        string Rename(string original, int index)
        {
            string name = original;

            if (!string.IsNullOrEmpty(find))
                name = name.Replace(find, replace);

            if (useRegex && !string.IsNullOrEmpty(regexPattern))
            {
                try
                {
                    name = Regex.Replace(
                        name,
                        regexPattern,
                        regexReplace);
                }
                catch { }
            }

            switch (caseMode)
            {
                case CaseMode.Upper: name = name.ToUpper(); break;
                case CaseMode.Lower: name = name.ToLower(); break;
                case CaseMode.Title:
                    name = System.Globalization
                        .CultureInfo.CurrentCulture
                        .TextInfo
                        .ToTitleCase(name);
                    break;
            }

            if (numbering)
            {
                string number =
                    (numberStart + index)
                    .ToString()
                    .PadLeft(padding, '0');

                name += "_" + number;
            }

            return prefix + name + suffix;
        }

        // ======================================================
        // ⭐ CORE PART
        // ======================================================

        void ApplyRename()
        {
            Undo.RecordObjects(targets, "Batch Rename");

            for (int i = 0; i < targets.Length; i++)
            {
                RenameObject(targets[i], _previewNames[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void RenameObject(Object obj, string newName)
        {
            if (obj == null)
                return;

            string path = AssetDatabase.GetAssetPath(obj);

            bool isMainAsset =
                AssetDatabase.LoadMainAssetAtPath(path) == obj;

            // MAIN ASSET
            if (isMainAsset && !string.IsNullOrEmpty(path))
            {
                AssetDatabase.RenameAsset(path, newName);
                return;
            }

            // SUB ASSET OR SCENE OBJECT
            obj.name = newName;
            EditorUtility.SetDirty(obj);
        }
    }
}
#endif
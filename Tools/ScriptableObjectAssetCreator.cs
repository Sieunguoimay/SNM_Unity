#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace SNMTools
{

    [EditorWindowTitle(title = "ScriptableObjectAssetCreator")]
    public class ScriptableObjectAssetCreator : EditorWindow
    {
        private System.Type[] _types;
        private readonly List<System.Type> _searchResult = new();
        private Vector2 _scrollPos;
        private string _searchString;
        private Action _assetCreatedCallback;
        private bool _focused;
        private UnityEngine.Object _selectedObject;
        private string _selectedPath;

        [MenuItem("Tools/ScriptableObjectAssetCreator")]
        public static void OpenWindow()
        {
            EditorWindow.GetWindow<ScriptableObjectAssetCreator>().Show();
        }

        [MenuItem("Assets/ScriptableObjectAssetCreator")]
        public static void OpenWindowFromAssets()
        {
            EditorWindow.GetWindow<ScriptableObjectAssetCreator>().Show();
        }

        public void SetAssetCreatedCallback(Action assetCreatedCallback)
        {
            _assetCreatedCallback = assetCreatedCallback;
        }

        private void OnEnable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            TryLoadSOTypes();

            DrawSavePath();

            GUI.SetNextControlName("ABCXYZ");
            var newString = GUILayout.TextField(_searchString);
            if (!_focused)
            {
                _focused = true;
                EditorGUI.FocusTextInControl("ABCXYZ");
            }

            if (_searchString != newString)
            {
                _searchString = newString;
                UpdateSearchResult();
            }
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var t in _searchResult)
            {
                if (GUILayout.Button($"{t.FullName}"))
                {
                    CreateAsset(t);
                    _assetCreatedCallback?.Invoke();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSavePath()
        {
            if (_selectedObject != Selection.activeObject)
            {
                _selectedObject = Selection.activeObject;
                _selectedPath = AssetDatabase.GetAssetPath(_selectedObject);
                var isSubAsset = AssetDatabase.IsValidFolder(_selectedPath) ? "" : " (Sub)";
                var segments = _selectedPath.Split("/");
                var dotDot = segments.Length > 2 ? "../" : "";
                _selectedPath = dotDot + string.Join("/", segments.TakeLast(2)) + isSubAsset;
            }

            EditorGUILayout.LabelField(_selectedPath);
        }

        private void UpdateSearchResult()
        {
            var regex = new Regex(string.Join(".*", _searchString.Split(" ")), RegexOptions.IgnoreCase);
            _searchResult.Clear();
            _searchResult.AddRange(_types.Where(t => regex.IsMatch(t.FullName)));
        }

        private void CreateAsset(Type type)
        {
            var so = CreateInstance(type);
            so.name = type.Name;

            var selectedObject = Selection.activeObject;
            var path = AssetDatabase.GetAssetPath(selectedObject);
            if (AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateAsset(so, System.IO.Path.Combine(path, type.Name + ".asset"));
            }
            else
            {
                AssetDatabase.AddObjectToAsset(so, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void TryLoadSOTypes()
        {
            if (_types == null)
            {
                LoadSOTypes();

                _searchString = "";
                UpdateSearchResult();
            }
        }

        private void LoadSOTypes()
        {
            _types = GetSOTypes().OrderBy(t => t.Name).ToArray();
        }

        private IEnumerable<Type> GetSOTypes()
        {
            var textAssets = AssetDatabase.FindAssets("t: MonoScript", new[] { "Assets" });
            return textAssets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TextAsset>)
                .OfType<MonoScript>()
                .Select(ms => ms.GetClass())
                .Where(ms => ms != null && typeof(ScriptableObject).IsAssignableFrom(ms))
                .Where(ms => !typeof(EditorWindow).IsAssignableFrom(ms))
                .Where(ms => !typeof(Editor).IsAssignableFrom(ms));
        }
    }
}
#endif
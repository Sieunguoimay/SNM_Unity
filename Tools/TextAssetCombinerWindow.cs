#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class TextAssetCombinerWindow : EditorWindow
{
    // ── Data ───────────────────────────────────────────────────────────────
    private enum AssetType { TextAsset, Shader }

    private class AssetEntry
    {
        public Object Asset;
        public AssetType Type;

        public string DisplayName => Asset != null ? Asset.name : "(none)";

        public string GetText()
        {
            if (Asset == null) return null;
            if (Type == AssetType.TextAsset)
                return ((TextAsset)Asset).text;
            // Shader: read raw source via AssetDatabase
            string path = AssetDatabase.GetAssetPath(Asset);
            return File.Exists(path) ? File.ReadAllText(path) : $"// Could not read shader source: {path}";
        }
    }

    // ── State ──────────────────────────────────────────────────────────────
    private List<AssetEntry> _entries = new List<AssetEntry>();
    private string _combinedText = "";
    private Vector2 _listScroll;
    private Vector2 _previewScroll;

    private string _separator = "\n\n---\n\n";
    private bool _includeFilenames = true;
    private bool _showPreview = true;

    private GUIStyle _textAreaStyle;
    private bool _stylesInitialised;

    // ── Menu Item ──────────────────────────────────────────────────────────
    [MenuItem("Tools/Snm/Assets/Text Asset Combiner")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextAssetCombinerWindow>("Text Asset Combiner");
        window.minSize = new Vector2(460, 540);
    }

    // ── GUI ────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        DrawHeader();
        EditorGUILayout.Space(4);

        DrawAssetList();
        EditorGUILayout.Space(6);

        DrawOptions();
        EditorGUILayout.Space(6);

        DrawActionButtons();
        EditorGUILayout.Space(6);

        if (_showPreview)
            DrawPreview();
    }

    // ── Header ─────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Text Asset Combiner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Drag TextAssets or Shaders below, combine them and copy to clipboard.",
            EditorStyles.miniLabel);
        DrawHorizontalLine();
    }

    // ── Asset list ─────────────────────────────────────────────────────────
    private void DrawAssetList()
    {
        EditorGUILayout.LabelField("Assets  (TextAsset & Shader)", EditorStyles.boldLabel);

        // Drop-zone
        Rect dropRect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Drop TextAssets or Shaders here  ✦", EditorStyles.helpBox);
        HandleDragAndDrop(dropRect);

        // Scrollable list
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll,
            GUILayout.Height(Mathf.Clamp(_entries.Count * 24 + 8, 40, 180)));

        int removeIndex = -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            EditorGUILayout.BeginHorizontal();

            // Up / Down arrows
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(18)))
            {
                (_entries[i - 1], _entries[i]) = (_entries[i], _entries[i - 1]);
                RebuildCombined();
            }
            GUI.enabled = i < _entries.Count - 1;
            if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(18)))
            {
                (_entries[i + 1], _entries[i]) = (_entries[i], _entries[i + 1]);
                RebuildCombined();
            }
            GUI.enabled = true;

            // Type badge
            var badgeStyle = entry.Type == AssetType.Shader ? GetShaderBadgeStyle() : GetTextBadgeStyle();
            GUILayout.Label(entry.Type == AssetType.Shader ? "HLSL" : "TXT", badgeStyle,
                GUILayout.Width(36), GUILayout.Height(18));

            // Object field — accept either type
            System.Type fieldType = entry.Type == AssetType.Shader ? typeof(Shader) : typeof(TextAsset);
            var updated = EditorGUILayout.ObjectField(entry.Asset, fieldType, false);
            if (updated != entry.Asset)
            {
                entry.Asset = updated;
                RebuildCombined();
            }

            // Remove
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            _entries.RemoveAt(removeIndex);
            RebuildCombined();
        }

        EditorGUILayout.EndScrollView();

        // Manual add / clear row
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Text Slot", GUILayout.Height(20)))
            _entries.Add(new AssetEntry { Type = AssetType.TextAsset });
        if (GUILayout.Button("+ Shader Slot", GUILayout.Height(20)))
            _entries.Add(new AssetEntry { Type = AssetType.Shader });
        if (GUILayout.Button("Clear All", GUILayout.Height(20)))
        {
            _entries.Clear();
            _combinedText = "";
        }
        EditorGUILayout.EndHorizontal();
    }

    // ── Options ────────────────────────────────────────────────────────────
    private void DrawOptions()
    {
        DrawHorizontalLine();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        _includeFilenames = EditorGUILayout.Toggle(
            new GUIContent("Include Filenames",
                "Prepend each asset's filename as a heading."),
            _includeFilenames);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Separator",
                "Text inserted between each asset."),
            GUILayout.Width(80));
        _separator = EditorGUILayout.TextField(_separator);
        if (GUILayout.Button("↺", GUILayout.Width(24)))
            _separator = "\n\n---\n\n";
        EditorGUILayout.EndHorizontal();

        _showPreview = EditorGUILayout.Toggle("Show Preview", _showPreview);

        if (EditorGUI.EndChangeCheck())
            RebuildCombined();
    }

    // ── Action buttons ─────────────────────────────────────────────────────
    private void DrawActionButtons()
    {
        DrawHorizontalLine();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("⟳  Rebuild", GUILayout.Height(28)))
            RebuildCombined();

        GUI.enabled = !string.IsNullOrEmpty(_combinedText);
        if (GUILayout.Button("⎘  Copy to Clipboard", GUILayout.Height(28)))
        {
            GUIUtility.systemCopyBuffer = _combinedText;
            ShowNotification(new GUIContent("Copied!"));
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // Stats
        if (!string.IsNullOrEmpty(_combinedText))
        {
            int validCount = 0;
            int shaderCount = 0;
            foreach (var e in _entries)
            {
                if (e.Asset == null) continue;
                validCount++;
                if (e.Type == AssetType.Shader) shaderCount++;
            }
            int textCount = validCount - shaderCount;
            EditorGUILayout.LabelField(
                $"{textCount} text  ·  {shaderCount} shader(s)  ·  {_combinedText.Length:N0} chars  ·  {CountLines(_combinedText):N0} lines",
                EditorStyles.centeredGreyMiniLabel);
        }
    }

    // ── Preview ────────────────────────────────────────────────────────────
    private void DrawPreview()
    {
        DrawHorizontalLine();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        float remaining = position.height
            - GUILayoutUtility.GetLastRect().yMax
            - 12f;

        _previewScroll = EditorGUILayout.BeginScrollView(
            _previewScroll, GUILayout.Height(Mathf.Max(remaining, 80)));

        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(_combinedText)
                ? "(no content – add assets and click Rebuild)"
                : _combinedText,
            _textAreaStyle,
            GUILayout.ExpandHeight(true),
            GUILayout.ExpandWidth(true));

        EditorGUILayout.EndScrollView();
    }

    // ── Core logic ─────────────────────────────────────────────────────────
    private void RebuildCombined()
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var entry in _entries)
        {
            if (entry.Asset == null) continue;
            string text = entry.GetText();
            if (text == null) continue;

            if (!first) sb.Append(_separator);
            first = false;

            if (_includeFilenames)
            {
                string ext = entry.Type == AssetType.Shader ? ".shader" : "";
                sb.AppendLine($"### {entry.Asset.name}{ext} ###");
            }

            sb.Append(text);
        }

        _combinedText = sb.ToString();
        Repaint();
    }

    private void HandleDragAndDrop(Rect dropRect)
    {
        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is TextAsset ta && !AlreadyContains(ta))
                    _entries.Add(new AssetEntry { Asset = ta, Type = AssetType.TextAsset });
                else if (obj is Shader sh && !AlreadyContains(sh))
                    _entries.Add(new AssetEntry { Asset = sh, Type = AssetType.Shader });
            }
            RebuildCombined();
            evt.Use();
        }
    }

    private bool AlreadyContains(Object obj)
    {
        foreach (var e in _entries)
            if (e.Asset == obj) return true;
        return false;
    }

    // ── Style helpers ───────────────────────────────────────────────────────
    private void InitStyles()
    {
        if (_stylesInitialised) return;
        _textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            richText = false,
            fontSize = 11,
            stretchHeight = true,
        };
        _stylesInitialised = true;
    }

    private static GUIStyle _shaderBadgeStyle;
    private static GUIStyle _textBadgeStyle;

    private static GUIStyle GetShaderBadgeStyle()
    {
        if (_shaderBadgeStyle == null)
        {
            _shaderBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.9f, 1f) },
                fontStyle = FontStyle.Bold,
            };
        }
        return _shaderBadgeStyle;
    }

    private static GUIStyle GetTextBadgeStyle()
    {
        if (_textBadgeStyle == null)
        {
            _textBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 1f, 0.6f) },
                fontStyle = FontStyle.Bold,
            };
        }
        return _textBadgeStyle;
    }

    private static void DrawHorizontalLine()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.6f));
    }

    private static int CountLines(string s)
    {
        int n = 1;
        foreach (char c in s) if (c == '\n') n++;
        return n;
    }
}
#endif
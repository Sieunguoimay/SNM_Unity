using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class TextAssetCombinerWindow : EditorWindow
{
    // ── State ──────────────────────────────────────────────────────────────
    private List<TextAsset> _textAssets = new List<TextAsset>();
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
        window.minSize = new Vector2(420, 520);
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
        EditorGUILayout.LabelField("Drag TextAssets below, combine them and copy to clipboard.",
            EditorStyles.miniLabel);
        DrawHorizontalLine();
    }

    // ── Asset list ─────────────────────────────────────────────────────────
    private void DrawAssetList()
    {
        EditorGUILayout.LabelField("Text Assets", EditorStyles.boldLabel);

        // Drop-zone
        Rect dropRect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Drop TextAssets here  ✦", EditorStyles.helpBox);
        HandleDragAndDrop(dropRect);

        // Scrollable list
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll,
            GUILayout.Height(Mathf.Clamp(_textAssets.Count * 24 + 8, 40, 160)));

        int removeIndex = -1;
        for (int i = 0; i < _textAssets.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            // Re-orderable drag handle (up/down arrows)
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(18)))
            {
                (_textAssets[i - 1], _textAssets[i]) = (_textAssets[i], _textAssets[i - 1]);
                RebuildCombined();
            }
            GUI.enabled = i < _textAssets.Count - 1;
            if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(18)))
            {
                (_textAssets[i + 1], _textAssets[i]) = (_textAssets[i], _textAssets[i + 1]);
                RebuildCombined();
            }
            GUI.enabled = true;

            var updated = (TextAsset)EditorGUILayout.ObjectField(
                _textAssets[i], typeof(TextAsset), false);
            if (updated != _textAssets[i])
            {
                _textAssets[i] = updated;
                RebuildCombined();
            }

            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            _textAssets.RemoveAt(removeIndex);
            RebuildCombined();
        }

        EditorGUILayout.EndScrollView();

        // Manual add / clear row
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Slot", GUILayout.Height(20)))
            _textAssets.Add(null);
        if (GUILayout.Button("Clear All", GUILayout.Height(20)))
        {
            _textAssets.Clear();
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
            foreach (var a in _textAssets) if (a != null) validCount++;
            EditorGUILayout.LabelField(
                $"{validCount} asset(s)  ·  {_combinedText.Length:N0} chars  ·  {CountLines(_combinedText):N0} lines",
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

        // Selectable, read-only textarea
        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(_combinedText)
                ? "(no content – add assets and click Rebuild)"
                : _combinedText,
            _textAreaStyle,
            GUILayout.ExpandHeight(true),
            GUILayout.ExpandWidth(true));

        EditorGUILayout.EndScrollView();
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private void RebuildCombined()
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var asset in _textAssets)
        {
            if (asset == null) continue;

            if (!first) sb.Append(_separator);
            first = false;

            if (_includeFilenames)
                sb.AppendLine($"### {asset.name} ###");

            sb.Append(asset.text);
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
                if (obj is TextAsset ta && !_textAssets.Contains(ta))
                    _textAssets.Add(ta);
            }
            RebuildCombined();
            evt.Use();
        }
    }

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

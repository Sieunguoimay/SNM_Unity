#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class ConsoleLogCollectorWindow : EditorWindow
    {
        [MenuItem("Tools/SNM/Console Log Collector")]
        static void Open() => GetWindow<ConsoleLogCollectorWindow>("Log Collector");

        private bool _includeStackTrace;
        private bool _collectLogs = true;
        private bool _collectWarnings = true;
        private bool _collectErrors = true;
        private Vector2 _scroll;
        private readonly List<LogEntry> _entries = new();

        private static Type _logEntriesType;
        private static MethodInfo _startMethod;
        private static MethodInfo _endMethod;
        private static MethodInfo _getEntryMethod;
        private static Type _logEntryType;
        private static FieldInfo _messageField;
        private static FieldInfo _fileField;
        private static FieldInfo _lineField;
        private static FieldInfo _modeField;
        private static bool _reflectionReady;

        private struct LogEntry
        {
            public string Message;
            public string FilePath;
            public int Mode;
        }

        static void EnsureReflection()
        {
            if (_reflectionReady) return;

            var asm = typeof(EditorWindow).Assembly;
            _logEntriesType = asm.GetType("UnityEditor.LogEntries") ?? asm.GetType("UnityEditorInternal.LogEntries");
            _logEntryType = asm.GetType("UnityEditor.LogEntry") ?? asm.GetType("UnityEditorInternal.LogEntry");

            if (_logEntriesType == null || _logEntryType == null) return;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _startMethod = _logEntriesType.GetMethod("StartGettingEntries", flags);
            _endMethod = _logEntriesType.GetMethod("EndGettingEntries", flags);
            _getEntryMethod = _logEntriesType.GetMethod("GetEntryInternal", flags);

            const BindingFlags instFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _messageField = _logEntryType.GetField("message", instFlags) ?? _logEntryType.GetField("condition", instFlags);
            _fileField = _logEntryType.GetField("file", instFlags);
            _lineField = _logEntryType.GetField("line", instFlags);
            _modeField = _logEntryType.GetField("mode", instFlags);

            _reflectionReady = _startMethod != null && _endMethod != null && _getEntryMethod != null && _messageField != null;
        }

        void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawLogList();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Capture", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                CaptureConsoleLogs();
            }

            _includeStackTrace = GUILayout.Toggle(_includeStackTrace, "Include Path", EditorStyles.toolbarButton, GUILayout.Width(90));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"{_entries.Count} entries", EditorStyles.miniLabel, GUILayout.Width(80));

            if (GUILayout.Button("Copy All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                CopyToClipboard();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _entries.Clear();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _collectLogs = GUILayout.Toggle(_collectLogs, "Log", EditorStyles.toolbarButton, GUILayout.Width(40));
            _collectWarnings = GUILayout.Toggle(_collectWarnings, "Warn", EditorStyles.toolbarButton, GUILayout.Width(45));
            _collectErrors = GUILayout.Toggle(_collectErrors, "Error", EditorStyles.toolbarButton, GUILayout.Width(45));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void CaptureConsoleLogs()
        {
            EnsureReflection();
            if (!_reflectionReady)
            {
                Debug.LogError("ConsoleLogCollector: Could not access LogEntries via reflection.");
                return;
            }

            _entries.Clear();
            var count = (int)_startMethod.Invoke(null, null);
            var entryObj = Activator.CreateInstance(_logEntryType);
            var args = new object[] { 0, entryObj };

            for (var i = 0; i < count; i++)
            {
                args[0] = i;
                _getEntryMethod.Invoke(null, args);
                entryObj = args[1];

                var mode = _modeField != null ? (int)_modeField.GetValue(entryObj) : 0;
                var isError = (mode & 0x101) != 0;
                var isWarning = (mode & 0x2) != 0;
                var isLog = !isError && !isWarning;

                if (isLog && !_collectLogs) continue;
                if (isWarning && !_collectWarnings) continue;
                if (isError && !_collectErrors) continue;

                var message = (string)_messageField.GetValue(entryObj) ?? "";
                var file = _fileField != null ? (string)_fileField.GetValue(entryObj) : null;
                var line = _lineField != null ? (int)_lineField.GetValue(entryObj) : 0;
                var filePath = !string.IsNullOrEmpty(file) ? $"{file}:{line}" : "";

                _entries.Add(new LogEntry
                {
                    Message = message,
                    FilePath = filePath,
                    Mode = mode
                });
            }

            _endMethod.Invoke(null, null);
        }

        void DrawLogList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var isError = (entry.Mode & 0x101) != 0;
                var isWarning = (entry.Mode & 0x2) != 0;

                var icon = isError ? "console.erroricon.sml"
                    : isWarning ? "console.warnicon.sml"
                    : "console.infoicon.sml";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(EditorGUIUtility.IconContent(icon), GUILayout.Width(20), GUILayout.Height(18));

                var text = _includeStackTrace && !string.IsNullOrEmpty(entry.FilePath)
                    ? $"{entry.Message}  ({entry.FilePath})"
                    : entry.Message;

                EditorGUILayout.SelectableLabel(text, EditorStyles.miniLabel, GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void CopyToClipboard()
        {
            var sb = new StringBuilder();
            foreach (var entry in _entries)
            {
                var isError = (entry.Mode & 0x101) != 0;
                var isWarning = (entry.Mode & 0x2) != 0;

                var prefix = isError ? "[ERROR]"
                    : isWarning ? "[WARN]"
                    : "[LOG]";

                sb.Append(prefix).Append(' ').Append(entry.Message);
                if (_includeStackTrace && !string.IsNullOrEmpty(entry.FilePath))
                {
                    sb.Append("  (").Append(entry.FilePath).Append(')');
                }
                sb.AppendLine();
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }
    }
}
#endif

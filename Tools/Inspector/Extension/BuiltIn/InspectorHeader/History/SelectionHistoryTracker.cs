#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryTracker : IDisposable
    {
        private readonly List<UnityEngine.Object> history = new();
        private int cursor = -1;
        private bool navigating;

        private const int MaxHistory = 100;

        public IReadOnlyList<UnityEngine.Object> History => history;
        public int Cursor => cursor;
        public bool CanGoBack => cursor > 0;
        public bool CanGoForward => cursor < history.Count - 1;

        public event Action OnHistoryChanged;

        public SelectionHistoryTracker()
        {
            Selection.selectionChanged += OnSelectionChanged;
            LoadHistory();
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            SaveHistory();
            history.Clear();
        }

        private void OnSelectionChanged()
        {
            if (navigating) return;

            var current = Selection.activeObject;
            if (current == null) return;

            // Don't add duplicate if already at current cursor position
            if (cursor >= 0 && cursor < history.Count && history[cursor] == current)
                return;

            CleanNulls();

            // Truncate forward history
            if (cursor < history.Count - 1)
                history.RemoveRange(cursor + 1, history.Count - cursor - 1);

            history.Add(current);

            // Trim oldest if over limit
            if (history.Count > MaxHistory)
            {
                history.RemoveAt(0);
            }

            cursor = history.Count - 1;

            OnHistoryChanged?.Invoke();
            SaveHistory();
        }

        public void GoBack()
        {
            if (!CanGoBack) return;
            NavigateTo(cursor - 1);
        }

        public void GoForward()
        {
            if (!CanGoForward) return;
            NavigateTo(cursor + 1);
        }

        public void NavigateTo(int index)
        {
            if (index < 0 || index >= history.Count) return;

            // Skip null entries
            var obj = history[index];
            if (obj == null)
            {
                history.RemoveAt(index);
                if (cursor >= history.Count) cursor = history.Count - 1;
                OnHistoryChanged?.Invoke();
                return;
            }

            cursor = index;
            navigating = true;
            Selection.activeObject = history[cursor];
            navigating = false;

            OnHistoryChanged?.Invoke();
        }

        public void ClearHistory()
        {
            history.Clear();
            cursor = -1;
            OnHistoryChanged?.Invoke();
            SaveHistory();
        }

        private void CleanNulls()
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i] == null)
                {
                    history.RemoveAt(i);
                    if (cursor > i) cursor--;
                }
            }

            if (cursor >= history.Count) cursor = history.Count - 1;
        }

        private void SaveHistory()
        {
            var guids = new List<string>();
            var savedCursor = cursor;
            var offset = 0;

            for (int i = 0; i < history.Count; i++)
            {
                var obj = history[i];
                if (obj == null) { if (i <= cursor) offset++; continue; }

                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) { if (i <= cursor) offset++; continue; }

                var guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                    guids.Add(guid);
            }

            savedCursor -= offset;
            if (savedCursor < 0) savedCursor = guids.Count - 1;
            if (savedCursor >= guids.Count) savedCursor = guids.Count - 1;

            EditorPrefs.SetString("SelectionHistoryTracker.History", string.Join("|", guids));
            EditorPrefs.SetInt("SelectionHistoryTracker.Cursor", savedCursor);
        }

        private void LoadHistory()
        {
            history.Clear();
            var data = EditorPrefs.GetString("SelectionHistoryTracker.History", "");

            if (!string.IsNullOrEmpty(data))
            {
                var guids = data.Split('|');
                foreach (var guid in guids)
                {
                    if (string.IsNullOrEmpty(guid)) continue;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null)
                        history.Add(obj);
                }
            }

            cursor = EditorPrefs.GetInt("SelectionHistoryTracker.Cursor", history.Count - 1);
            if (cursor >= history.Count) cursor = history.Count - 1;
        }
    }
}
#endif

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

        // Legacy EditorPrefs keys from when history was persisted across sessions.
        // Deleted on construction so old data doesn't sit in EditorPrefs forever.
        private const string LegacyHistoryKey = "SelectionHistoryTracker.History";
        private const string LegacyCursorKey = "SelectionHistoryTracker.Cursor";

        public IReadOnlyList<UnityEngine.Object> History => history;
        public int Cursor => cursor;
        public bool CanGoBack => cursor > 0;
        public bool CanGoForward => cursor < history.Count - 1;

        public event Action OnHistoryChanged;

        public SelectionHistoryTracker()
        {
            EditorPrefs.DeleteKey(LegacyHistoryKey);
            EditorPrefs.DeleteKey(LegacyCursorKey);
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
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
    }
}
#endif

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryTracker : IDisposable
    {
        private readonly List<UnityEngine.Object> allHistory = new();
        private readonly List<UnityEngine.Object> quickAccessHistory = new();
        private int _maxQuickAccessHistoryCount;

        public IReadOnlyList<UnityEngine.Object> AllHistory => allHistory;
        public IReadOnlyList<UnityEngine.Object> QuickAccessHistory => quickAccessHistory;

        public int MaxQuickAccessHistoryCount => _maxQuickAccessHistoryCount;

        public event Action OnSelectionHistoryChanged;

        public SelectionHistoryTracker()
        {
            Selection.selectionChanged += Selection_OnSelectionChanged;
            EditorApplication.playModeStateChanged += EditorApplication_OnPlayModeStateChanged;
            LoadHistory();
            CaptureHistory();
        }

        public void Dispose()
        {
            Selection.selectionChanged -= Selection_OnSelectionChanged;
            EditorApplication.playModeStateChanged -= EditorApplication_OnPlayModeStateChanged;
            SaveHistory();
            allHistory.Clear();
        }

        private void EditorApplication_OnPlayModeStateChanged(PlayModeStateChange change)
        {
            ClearHistory();
        }

        private void Selection_OnSelectionChanged()
        {
            CaptureHistory();
        }

        private void CaptureHistory()
        {
            var current = Selection.activeObject;
            if (current != null)
            {
                CaptureHistory(current);
                CaptureQuickAccessHistory(current);
                OnSelectionHistoryChanged?.Invoke();
                SaveHistory();
            }
        }

        private void CaptureHistory(UnityEngine.Object current)
        {
            if (allHistory.Contains(current))
            {
                allHistory.Remove(current);
            }
            allHistory.Add(current);

            //limit the history to 100 items
            if (allHistory.Count > 100)
            {
                allHistory.RemoveAt(0);
            }
        }

        private void CaptureQuickAccessHistory(UnityEngine.Object current)
        {
            if (!quickAccessHistory.Contains(current))
            {
                quickAccessHistory.Add(current);
            }

            if (quickAccessHistory.Count > _maxQuickAccessHistoryCount)
            {
                quickAccessHistory.RemoveAt(0);
            }
        }

        public void SetMaxQuickAccessHistoryCount(int count)
        {
            if (count < 1) return;
            _maxQuickAccessHistoryCount = count;
            while (quickAccessHistory.Count > _maxQuickAccessHistoryCount)
            {
                quickAccessHistory.RemoveAt(0);
            }
            SaveHistory();
        }

        public void ClearHistory()
        {
            allHistory.Clear();
            quickAccessHistory.Clear();
            OnSelectionHistoryChanged?.Invoke();
            SaveHistory();
        }

        private void SaveHistory()
        {
            var guids = allHistory
                .ConvertAll(AssetDatabase.GetAssetPath)
                .ConvertAll(AssetDatabase.AssetPathToGUID);
            EditorPrefs.SetString("SelectionHistoryTracker.AllHistory", string.Join("|", guids));
            EditorPrefs.SetInt("SelectionHistoryTracker.MaxQuickAccessHistoryCount", _maxQuickAccessHistoryCount);
        }

        private void LoadHistory()
        {
            _maxQuickAccessHistoryCount = EditorPrefs.GetInt("SelectionHistoryTracker.MaxQuickAccessHistoryCount", 3);
            if (_maxQuickAccessHistoryCount < 1) _maxQuickAccessHistoryCount = 3;

            allHistory.Clear();
            var guidsStr = EditorPrefs.GetString("SelectionHistoryTracker.AllHistory", "");

            if (!string.IsNullOrEmpty(guidsStr))
            {
                var guids = guidsStr.Split('|');
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null)
                    {
                        allHistory.Add(obj);
                    }
                }
            }
        }
    }
}
#endif
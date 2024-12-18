#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InspectorExtensions
{
    public interface IHistoryList
    {
        int HistoryCount { get; }
        IEnumerable<string> GetHistoryDisplay();
        bool IsHistoryCurrent(int index);
        UnityEngine.Object GetObjectFromHistory(int index);
    }

    public interface IHistoryBrowser
    {
        string Next { get; }
        string Prev { get; }

        bool CanNavigateBack { get; }
        bool CanNavigateForward { get; }

        event Action<HistoryBrowsingData> OnHistoryChanged;
        void SetEnabled(bool enabled);
        void ClearHistory();
        void Navigate(int direction);
    }

    public class HistoryBrowsingData : IDisposable, IHistoryBrowser, IHistoryList
    {
        private const int MAX_HISTORY = 10;
        private const string HISTORY_KEY = "InspectorHistoryTracker_History";
        private const string CURRENT_INDEX_KEY = "InspectorHistoryTracker_CurrentIndex";
        private const string IS_ENABLED_KEY = "InspectorHistoryTracker_IsEnabled";
        private const string IS_DEBUG_ENABLED_KEY = "InspectorHistoryTracker_IsDebugEnabled";

        private readonly List<int> navigation = new();
        private readonly List<ObjectInfo> history = new();

        private bool _skipUpdateNavigation = false;
        private Action<HistoryBrowsingData> _onHistoryChanged;

        private int CurrentNavIndex
        {
            set => EditorPrefs.SetInt(CURRENT_INDEX_KEY, value);
            get => EditorPrefs.GetInt(CURRENT_INDEX_KEY, -1);
        }

        public bool IsEnabled
        {
            set => EditorPrefs.SetBool(IS_ENABLED_KEY, value);
            get => EditorPrefs.GetBool(IS_ENABLED_KEY, false);
        }

        public bool IsDebugEnabled
        {
            set => EditorPrefs.SetBool(IS_DEBUG_ENABLED_KEY, value);
            get => EditorPrefs.GetBool(IS_DEBUG_ENABLED_KEY, false);
        }

        public HistoryBrowsingData()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            LoadHistory();
            UpdateNavigation();
        }

        public void Dispose()
        {
            SaveHistory();
            Selection.selectionChanged -= OnSelectionChanged;
            Cleanup();
        }

        private void OnSelectionChanged()
        {
            if (!IsEnabled) return;

            if (!_skipUpdateNavigation)
            {
                UpdateNavigation();
            }
        }

        private void UpdateNavigation()
        {
            var selected = ObjectInfoHelper.GetObjectInfo(Selection.activeObject);
            var selectedDisplay = GetGUIDDataDisplay(selected);

            if (IsDebugEnabled)
            {
                Debug.Log(selectedDisplay);
            }

            if (CurrentNavIndex >= 0 && GetNavigationDisplay(CurrentNavIndex) == selectedDisplay)
                return;

            if (CurrentNavIndex < navigation.Count - 1)
                navigation.RemoveRange(CurrentNavIndex + 1, navigation.Count - CurrentNavIndex - 1);

            if (CurrentNavIndex > navigation.Count - 1)
                CurrentNavIndex = navigation.Count - 1;

            if (navigation.Count > MAX_HISTORY - 1)
            {
                navigation.RemoveAt(0);
                CurrentNavIndex--;
            }

            navigation.Add(TryGetIndex(selected));
            CurrentNavIndex++;

            _onHistoryChanged?.Invoke(this);
            SaveHistory();
        }


        private ObjectInfo TryGetHistory(int index)
        {
            if (index >= 0 && index < history.Count)
            {
                return history[index];
            }
            return null;
        }

        private int TryGetIndex(ObjectInfo data)
        {
            for (var i = 0; i < history.Count; i++)
            {
                if (ObjectInfo.Equals(history[i], data))
                {
                    return i;
                }
            }
            history.Add(data);

            if (history.Count > MAX_HISTORY)
            {
                var newHistory = history.Where(h => navigation.Contains(history.IndexOf(h))).ToList();
                var mapFromOldToNew = newHistory.ToDictionary(h => history.IndexOf(h), h => newHistory.IndexOf(h));

                for (var i = 0; i < navigation.Count; i++)
                {
                    navigation[i] = mapFromOldToNew[navigation[i]];
                }

                history.Clear();
                history.AddRange(newHistory);
            }

            return history.Count - 1;
        }

        private void Cleanup()
        {
            history.Clear();
            navigation.Clear();
            CurrentNavIndex = -1;
        }

        private string GetNavigationDisplay(int index)
        {
            var valid = index >= 0 && index < navigation.Count;
            return GetGUIDDataDisplay(valid ? TryGetHistory(navigation[index]) : null);
        }

        private static string GetGUIDDataDisplay(ObjectInfo data)
        {
            return data?.Display ?? "NULL";
        }

        #region IHistoryBrowser
        string IHistoryBrowser.Next => GetNavigationDisplay(CurrentNavIndex + 1);
        string IHistoryBrowser.Prev => GetNavigationDisplay(CurrentNavIndex - 1);

        bool IHistoryBrowser.CanNavigateBack => CurrentNavIndex > 0;
        bool IHistoryBrowser.CanNavigateForward => CurrentNavIndex >= 0 && CurrentNavIndex < navigation.Count - 1;

        event Action<HistoryBrowsingData> IHistoryBrowser.OnHistoryChanged
        {
            add { _onHistoryChanged += value; }
            remove { _onHistoryChanged -= value; }
        }
        void IHistoryBrowser.SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        void IHistoryBrowser.Navigate(int direction)
        {
            int newIndex = CurrentNavIndex + direction;

            if (newIndex >= 0 && newIndex < navigation.Count)
            {
                CurrentNavIndex = newIndex;
                _skipUpdateNavigation = true;
                Selection.activeObject = ObjectInfoHelper.LoadObject(history[navigation[CurrentNavIndex]], _ => _skipUpdateNavigation = _);
                _skipUpdateNavigation = false;
                _onHistoryChanged?.Invoke(this);
            }
        }

        void IHistoryBrowser.ClearHistory()
        {
            Cleanup();
            _onHistoryChanged?.Invoke(this);
            SaveHistory();
        }
        #endregion

        #region IHistoryList
        int IHistoryList.HistoryCount => history.Count;

        UnityEngine.Object IHistoryList.GetObjectFromHistory(int index)
        {
            var valid = index >= 0 && index < history.Count;
            return valid ? ObjectInfoHelper.LoadObject(history[index], _ => _skipUpdateNavigation = _) : null;
        }

        IEnumerable<string> IHistoryList.GetHistoryDisplay()
        {
            return history.Select(GetGUIDDataDisplayBeautiful);
        }

        bool IHistoryList.IsHistoryCurrent(int index)
        {
            var valid = index >= 0 && index < history.Count;
            var valid2 = CurrentNavIndex >= 0 && CurrentNavIndex < navigation.Count;
            return valid && valid2 && navigation[CurrentNavIndex] == index;
        }

        private string GetGUIDDataDisplayBeautiful(ObjectInfo data)
        {
            if (data == null) return "NULL";

            var loaded = ObjectInfoHelper.LoadObject(data, _ => _skipUpdateNavigation = _);

            if (loaded != null)
            {
                return $"{data.Path} | {loaded.name}"; ;
            }

            return "NULL";
        }
        #endregion


        private void SaveHistory()
        {
            var json = JsonUtility.ToJson(new HistorySaveData { history = history, navigation = navigation });
            EditorPrefs.SetString(HISTORY_KEY, json);

            if (IsDebugEnabled)
            {
                Debug.Log("SaveHistory: " + json);
            }
        }

        private void LoadHistory()
        {
            if (EditorPrefs.HasKey(HISTORY_KEY))
            {
                var json = EditorPrefs.GetString(HISTORY_KEY);
                var data = JsonUtility.FromJson<HistorySaveData>(json);

                if (IsDebugEnabled)
                {
                    Debug.Log("LoadHistory: " + json);
                }

                navigation.Clear();
                history.Clear();

                navigation.AddRange(data.navigation);
                history.AddRange(data.history);
            }
        }

        [Serializable]
        private class HistorySaveData
        {
            public List<ObjectInfo> history;
            public List<int> navigation;
        }
    }
}

#endif
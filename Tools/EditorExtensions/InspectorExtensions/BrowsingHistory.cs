#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InspectorExtensions
{
    public class BrowsingHistory : IDisposable
    {
        private const int MAX_HISTORY = 10;
        private readonly List<GUIDData> history = new();
        private readonly List<GUIDData> navigation = new();
        private int currentIndex = -1;
        private bool _skipUpdateNavigation = false;
        public bool CanNavigateBack => currentIndex > 0;
        public bool CanNavigateForward => currentIndex >= 0 && currentIndex < navigation.Count - 1;

        public int HistoryCount => history.Count;

        public IEnumerable<string> History => history.Select(GetGUIDDataDisplay);

        public string Next => GetNavigationDisplay(currentIndex + 1);
        public string Prev => GetNavigationDisplay(currentIndex - 1);
        public string Current => GetNavigationDisplay(currentIndex);

        public event Action<BrowsingHistory> OnHistoryChanged;

        private bool _isEnabled = false;

        public BrowsingHistory()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            UpdateHistory();
            UpdateNavigation();
        }

        void IDisposable.Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            ClearHistory();
        }

        private void OnSelectionChanged()
        {
            if (!_isEnabled) return;

            UpdateHistory();

            if (_skipUpdateNavigation) return;
            UpdateNavigation();
        }

        private void UpdateHistory()
        {
            var selected = GetGUIDData(Selection.activeObject);
            var selectedDisplay = GetGUIDDataDisplay(selected);

            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (GetGUIDDataDisplay(history[i]) == selectedDisplay)
                {
                    history.RemoveAt(i);
                }
            }
            history.Add(selected);
        }

        private void UpdateNavigation()
        {
            var selected = GetGUIDData(Selection.activeObject);
            var selectedDisplay = GetGUIDDataDisplay(selected);
            selected?.Log();

            if (currentIndex >= 0 && GetNavigationDisplay(currentIndex) == selectedDisplay)
                return;

            if (currentIndex < navigation.Count - 1)
                navigation.RemoveRange(currentIndex + 1, navigation.Count - currentIndex - 1);

            navigation.Add(selected);
            currentIndex++;

            if (navigation.Count > MAX_HISTORY)
            {
                navigation.RemoveAt(0);
                currentIndex--;
            }

            OnHistoryChanged?.Invoke(this);
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public void Navigate(int direction)
        {
            int newIndex = currentIndex + direction;

            if (newIndex >= 0 && newIndex < navigation.Count)
            {
                currentIndex = newIndex;
                _skipUpdateNavigation = true;
                Selection.activeObject = LoadObject(navigation[currentIndex]);
                _skipUpdateNavigation = false;
                OnHistoryChanged?.Invoke(this);
            }
        }

        public void ClearHistory()
        {
            history.Clear();
            navigation.Clear();
            currentIndex = -1;
            OnHistoryChanged?.Invoke(this);
        }

        public UnityEngine.Object GetObjectFromHistory(int index)
        {
            var valid = index >= 0 && index < history.Count;
            return valid ? LoadObject(history[index]) : null;
        }

        private string GetNavigationDisplay(int index)
        {
            var valid = index >= 0 && index < navigation.Count;
            return GetGUIDDataDisplay(valid ? navigation[index] : null);
        }

        private static string GetGUIDDataDisplay(GUIDData data)
        {
            return data?.Display ?? "NULL";
        }

        // private const string HISTORY_KEY = "InspectorHistoryTracker_History";
        // private void SaveHistory()
        // {
        //     var json = JsonUtility.ToJson(new HistoryData(history.Select(o =>
        //     {
        //         if (o != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out var guid, out long localId))
        //         {
        //             return new GUIDData { guid = guid, localId = localId.ToString() };
        //         }
        //         return null;
        //     }).Where(o => o != null).ToArray(), currentIndex));

        //     EditorPrefs.SetString(HISTORY_KEY, json);

        //     Debug.Log("SaveHistory: " + json);
        // }

        // private void LoadHistory()
        // {
        //     if (EditorPrefs.HasKey(HISTORY_KEY))
        //     {
        //         var json = EditorPrefs.GetString(HISTORY_KEY);
        //         var data = JsonUtility.FromJson<HistoryData>(json);
        //         if (data != null)
        //         {
        //             history.Clear();
        //             history.AddRange(data.history.Select(d =>
        //                 AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(d.guid))
        //                 .FirstOrDefault(a => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var guid, out var localId) && localId == long.Parse(d.localId)))
        //                 .ToList());
        //             currentIndex = Mathf.Min(data.currentIndex, history.Count - 1);
        //         }

        //         Debug.Log("LoadHistory: " + json);
        //     }
        // }

        [System.Serializable]
        private class GUIDData
        {
            public string path;
            public string localId;
            public GUIDDataType guidDataType;
            public string Display => $"{path.Replace("/", "\u2215")}|{localId}";

            public void Log()
            {
                Debug.Log($"path: {path}, localPath: {localId}");
            }
        }

        private enum GUIDDataType
        {
            NonPrefabAsset,
            PrefabAsset,
            ObjectInPrefab,
            ObjectInScene,
        }

        private GUIDData GetGUIDData(UnityEngine.Object obj)
        {
            if (obj == null) return null;
            if (obj is GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                    {
                        var goLocalId = GetLocalID(go);
                        return new GUIDData
                        {
                            path = stage.assetPath,
                            localId = goLocalId,
                            guidDataType = GUIDDataType.ObjectInPrefab
                        };
                    }
                    else
                    {
                        var goLocalId = GetLocalID(go);
                        return new GUIDData
                        {
                            path = go.scene.path,
                            localId = goLocalId,
                            guidDataType = GUIDDataType.ObjectInScene
                        };
                    }
                }
                // else
                // {
                //     return new GUIDData
                //     {
                //         path = AssetDatabase.GetAssetPath(go),
                //         localId = "",
                //         guidDataType = GUIDDataType.PrefabAsset,
                //     };
                // }
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId))
            {
                return new GUIDData
                {
                    path = AssetDatabase.GUIDToAssetPath(guid),
                    localId = localId.ToString(),
                    guidDataType = GUIDDataType.NonPrefabAsset,
                };
            }

            return null;
        }

        private UnityEngine.Object LoadObject(GUIDData guidData)
        {
            if (guidData == null) return null;

            if (guidData.path.EndsWith(".unity") && guidData.guidDataType == GUIDDataType.ObjectInScene)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                var isCurrent = activeScene != null && activeScene.path == guidData.path;
                var scene = isCurrent ? activeScene : EditorSceneManager.OpenScene(guidData.path);

                foreach (var rgo in scene.GetRootGameObjects())
                {
                    foreach (var go in Iterate(rgo))
                    {
                        var goLocalId = GetLocalID(go);
                        if (guidData.localId == goLocalId)
                        {
                            return go;
                        }
                    }
                }
            }
            else if (guidData.path.EndsWith(".prefab") && guidData.guidDataType == GUIDDataType.ObjectInPrefab)
            {
                var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
                var isCurrent = currentStage != null && currentStage.assetPath == guidData.path;
                var stage = isCurrent ? currentStage : PrefabStageUtility.OpenPrefab(guidData.path);
                foreach (var go in Iterate(stage.prefabContentsRoot))
                {
                    var goLocalId = GetLocalID(go);
                    if (guidData.localId == goLocalId)
                    {
                        return go;
                    }
                }
            }
            // else if (guidData.guidDataType == GUIDDataType.PrefabAsset)
            // {
            //     return AssetDatabase.LoadAssetAtPath<GameObject>(guidData.path);
            // }
            else
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(guidData.path);
                var assetGUID = AssetDatabase.AssetPathToGUID(guidData.path);
                return allAssets.FirstOrDefault(a =>
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var guid, out var localId))
                    {
                        return guid == assetGUID && localId.ToString() == guidData.localId;
                    }
                    return false;
                });
            }
            return null;
        }

        private IEnumerable<GameObject> Iterate(GameObject go)
        {
            yield return go;
            foreach (Transform c in go.transform)
            {
                yield return c.gameObject;
            }
        }

        private string GetLocalID(UnityEngine.Object obj)
        {
            var inspectorModeInfo =
                typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

            var serializedObject = new SerializedObject(obj);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            var localIdProp =
                serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

            return localIdProp.boxedValue.ToString();
        }


        // [System.Serializable]
        // private class HistoryData
        // {
        //     public GUIDData[] history;
        //     public int currentIndex;

        //     public HistoryData(GUIDData[] history, int currentIndex)
        //     {
        //         this.history = history;
        //         this.currentIndex = currentIndex;
        //     }
        // }
    }
}

#endif
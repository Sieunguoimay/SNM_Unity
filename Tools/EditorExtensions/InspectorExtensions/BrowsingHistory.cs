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
        private int currentIndex = -1;

        public bool CanNavigateBack => currentIndex > 0;
        public bool CanNavigateForward => currentIndex < history.Count - 1;
        public int CurrentIndex => currentIndex;
        public IEnumerable<string> History => history.Select(GetHistoryDisplay);
        public string Next => currentIndex < history.Count - 1 ? GetHistoryDisplay(history[currentIndex + 1]) : "NULL";
        public string Prev => currentIndex > 0 ? GetHistoryDisplay(history[currentIndex - 1]) : "NULL";

        public event Action<BrowsingHistory> OnHistoryChanged;

        public BrowsingHistory()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            UpdateHistory();
        }

        void IDisposable.Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            UpdateHistory();
        }

        private void UpdateHistory()
        {
            var selectedObject = GetGUIDData(Selection.activeObject);

            selectedObject?.Log();

            if (currentIndex >= 0 && GetHistoryDisplay(history[currentIndex]) == GetHistoryDisplay(selectedObject))
                return;

            if (currentIndex < history.Count - 1)
                history.RemoveRange(currentIndex + 1, history.Count - currentIndex - 1);

            history.Add(selectedObject);
            currentIndex++;

            if (history.Count > MAX_HISTORY)
            {
                history.RemoveAt(0);
                currentIndex--;
            }

            OnHistoryChanged?.Invoke(this);
        }

        public void Navigate(int direction)
        {
            int newIndex = currentIndex + direction;

            if (newIndex >= 0 && newIndex < history.Count)
            {
                currentIndex = newIndex;
                Selection.activeObject = LoadObject(history[currentIndex]);
                OnHistoryChanged?.Invoke(this);
            }
        }

        public void SelectObjectAtIndex(int index)
        {
            currentIndex = index;
            Selection.activeObject = LoadObject(history[currentIndex]);
            OnHistoryChanged?.Invoke(this);
        }

        private static string GetHistoryDisplay(GUIDData data)
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
                else
                {
                    return new GUIDData
                    {
                        path = AssetDatabase.GetAssetPath(go),
                        localId = "",
                        guidDataType = GUIDDataType.PrefabAsset,
                    };
                }
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
                var scene = EditorSceneManager.OpenScene(guidData.path);
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
                var stage = PrefabStageUtility.OpenPrefab(guidData.path);
                foreach (var go in Iterate(stage.prefabContentsRoot))
                {
                    var goLocalId = GetLocalID(go);
                    if (guidData.localId == goLocalId)
                    {
                        return go;
                    }
                }
            }
            else if (guidData.guidDataType == GUIDDataType.PrefabAsset)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(guidData.path);
            }
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
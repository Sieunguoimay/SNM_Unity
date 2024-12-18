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
    public class ObjectInfoHelper
    {
        private static PropertyInfo inspectorModeInfo;
        private static PropertyInfo InspectorModeInfo => inspectorModeInfo ??=
            typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Dictionary<UnityEngine.Object, string> _localIDCache = new();

        public static ObjectInfo GetObjectInfo(UnityEngine.Object obj)
        {
            if (obj == null) return new ObjectInfo();
            if (obj is GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                    {
                        var goLocalId = GetLocalID(go);
                        return new ObjectInfo
                        {
                            path = stage.assetPath,
                            localId = goLocalId,
                            objectType = ObjectType.ObjectInPrefab
                        };
                    }
                    else
                    {
                        var goLocalId = GetLocalID(go);
                        return new ObjectInfo
                        {
                            path = go.scene.path,
                            localId = goLocalId,
                            objectType = ObjectType.ObjectInScene
                        };
                    }
                }
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localId))
            {
                return new ObjectInfo
                {
                    path = AssetDatabase.GUIDToAssetPath(guid),
                    localId = localId.ToString(),
                    objectType = ObjectType.NonPrefabAsset,
                };
            }

            return new ObjectInfo();
        }

        public static UnityEngine.Object LoadObject(ObjectInfo objectInfo, Action<bool> afterLoad)
        {
            if (objectInfo == null) return null;

            if (objectInfo.path.EndsWith(".unity"))
            {
                if (objectInfo.objectType == ObjectType.ObjectInScene)
                {
                    var activeScene = EditorSceneManager.GetActiveScene();
                    var isCurrent = activeScene != null && activeScene.path == objectInfo.path;
                    afterLoad?.Invoke(true);
                    var scene = isCurrent ? activeScene : EditorSceneManager.OpenScene(objectInfo.path);
                    afterLoad?.Invoke(false);

                    foreach (var rgo in scene.GetRootGameObjects())
                    {
                        foreach (var go in IterateGameobjectHierachy(rgo))
                        {
                            var goLocalId = GetLocalID(go);
                            if (objectInfo.localId == goLocalId)
                            {
                                return go;
                            }
                        }
                    }
                }
            }
            else if (objectInfo.path.EndsWith(".prefab") && objectInfo.objectType == ObjectType.ObjectInPrefab)
            {
                var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
                var isCurrent = currentStage != null && currentStage.assetPath == objectInfo.path;
                afterLoad?.Invoke(true);
                var stage = isCurrent ? currentStage : PrefabStageUtility.OpenPrefab(objectInfo.path);
                afterLoad?.Invoke(false);
                foreach (var go in IterateGameobjectHierachy(stage.prefabContentsRoot))
                {
                    var goLocalId = GetLocalID(go);
                    if (objectInfo.localId == goLocalId)
                    {
                        return go;
                    }
                }
            }
            else if (string.IsNullOrEmpty(objectInfo.path) == false)
            {
                if (AssetDatabase.IsValidFolder(objectInfo.path)) return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objectInfo.path);

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(objectInfo.path);
                var assetGUID = AssetDatabase.AssetPathToGUID(objectInfo.path);
                return allAssets.FirstOrDefault(a =>
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var guid, out var localId))
                    {
                        return guid == assetGUID && localId.ToString() == objectInfo.localId;
                    }
                    return false;
                });
            }
            return null;
        }

        private static IEnumerable<GameObject> IterateGameobjectHierachy(GameObject go)
        {
            yield return go;
            foreach (Transform c in go.transform)
            {
                yield return c.gameObject;
            }
        }

        private static string GetLocalID(UnityEngine.Object obj)
        {
            LimitLocalIDCacheSize();

            if (_localIDCache.TryGetValue(obj, out var localId))
            {
                return localId;
            }
            else
            {
                var serializedObject = new SerializedObject(obj);
                InspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
                var localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile").longValue.ToString();   //note the misspelling!

                _localIDCache.Add(obj, localIdProp);

                return localIdProp;
            }
        }

        private static void LimitLocalIDCacheSize()
        {
            if (_localIDCache.Count > 100)
            {
                _localIDCache = _localIDCache.Where(c => c.Key != null).ToDictionary(c => c.Key, c => c.Value);

                if (_localIDCache.Count > 100)
                {
                    _localIDCache.Clear();
                }
            }
        }

    }
}

#endif
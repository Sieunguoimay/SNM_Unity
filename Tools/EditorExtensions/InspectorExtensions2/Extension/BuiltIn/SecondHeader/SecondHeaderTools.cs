#if UNITY_EDITOR
using System;
using System.IO;
using Snm.Tools.InspectorExtra;
using Snm.Tools.ObjectBrowser;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public static class SecondHeaderTools
    {
        public static void OpenObjectBrowser(UnityEngine.Object target)
        {
            EditorWindow.GetWindow<ObjectBrowserWindow>().Browse(target);
        }

        public static void OpenScript(UnityEngine.Object target)
        {
            if (target != null)
            {
                var serialized = new SerializedObject(target);
                var scriptProperty = serialized.FindProperty("m_Script");
                AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
            }
        }

        public static void OpenFindReferencesInScene(UnityEngine.Object target)
        {
            EditorWindow.GetWindow<SceneReferencesFinderWindow>().Find(target);
        }

        public static void FindRefrencesInProject(UnityEngine.Object target)
        {
            var targetPath = AssetDatabase.GetAssetPath(target);
            var guid = AssetDatabase.AssetPathToGUID(targetPath);

            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/"))
                    continue;

                if (AssetDatabase.IsValidFolder(path))
                    continue;

                string fullPath = Path.GetFullPath(path);

                if (!File.Exists(fullPath))
                    continue;

                try
                {
                    string text = File.ReadAllText(fullPath);
                    if (text.Contains(guid))
                    {
                        Debug.Log("Reference found in: " + path);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore protected files
                }
                catch (IOException)
                {
                    // Ignore binary or locked files
                }
            }
        }
    }

}
#endif

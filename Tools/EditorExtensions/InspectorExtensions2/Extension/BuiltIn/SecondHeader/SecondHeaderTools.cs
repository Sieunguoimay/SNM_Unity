#if UNITY_EDITOR
using System;
using System.IO;
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
            var window = EditorWindow.GetWindow<SceneReferenceFinderWindow>();
            window.target = target;
            window.Show();
            window.FindReferences();
        }

        public static void FindRefrencesInProject(UnityEngine.Object target)
        {
            var window = EditorWindow.GetWindow<AssetReferenceFinderWindow>("Reference Finder");
            window.targetRoots.Add(target);
            window.searchRoots.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets"));
            window.Show();
        }
    }

}
#endif

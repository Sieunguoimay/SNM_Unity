#if UNITY_EDITOR
using Snm.Tools.Finders;
using Snm.Tools.ObjectBrowser;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public static class SecondHeaderTools
    {
        public static void OpenObjectBrowser(Object target)
        {
            EditorWindow.GetWindow<ObjectBrowserWindow>().Browse(target);
        }

        public static void OpenScript(Object target)
        {
            if (target != null)
            {
                var serialized = new SerializedObject(target);
                var scriptProperty = serialized.FindProperty("m_Script");
                AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
            }
        }

        public static void OpenFindReferencesInScene(Object target)
        {
            ReferenceFinderWindow.OpenForScene(target);
        }

        public static void FindRefrencesInProject(Object target)
        {
            ReferenceFinderWindow.OpenForProject(target);
        }
    }
}
#endif

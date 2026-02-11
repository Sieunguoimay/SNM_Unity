#if UNITY_EDITOR
using System.Reflection;
using Snm.Tools.InspectorExtra;
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
            EditorWindow.GetWindow<SceneReferencesFinderWindow>().Find(target);
        }

        public static void FindRefrencesInProject(Object target)
        {
            typeof(SearchableEditorWindow)
                .GetMethod("SearchForReferencesInProject", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { target });
        }
    }

}
#endif

using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.App.Composition
{
    [CustomEditor(typeof(AppModuleAsset), true)]
    public class AppModuleAssetEditor : Editor
    {
        [MenuItem("Assets/GrabAndToss/Collect AppModules")]
        public static void CollectAppModulesFromAsset()
        {
            if (Selection.activeObject is not AppModuleAsset)
            {
                EditorUtility.DisplayDialog("Error", "Select a AppModuleAsset first", "OK");
                return;
            }

            AppModuleCollectorUtility.CollectAppModules();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AppModule Collection", EditorStyles.boldLabel);

            if (GUILayout.Button("Collect All AppModules", GUILayout.Height(30)))
            {
                AppModuleCollectorUtility.CollectAppModules();
            }

            EditorGUILayout.HelpBox(
                "Click 'Collect All AppModules' to find all AppModuleAsset ScriptableObjects and populate them into a AppModulesAsset.",
                MessageType.Info);
        }
    }
}

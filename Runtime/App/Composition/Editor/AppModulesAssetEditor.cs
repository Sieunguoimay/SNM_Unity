#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.App.Composition
{
    [CustomEditor(typeof(AppModulesAsset))]
    public class AppModulesAssetEditor : Editor
    {
        [MenuItem("Tools/GrabAndToss/Collect AppModules")]
        public static void CollectAppModulesMenuItem()
        {
            var registry = Selection.activeObject as AppModulesAsset;
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Error", "Select a AppModulesAsset first", "OK");
                return;
            }

            AppModuleCollectorUtility.CollectAppModulesForRegistry(registry);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AppModule Collection", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto-Collect AppModuleAssets", GUILayout.Height(30)))
            {
                AppModuleCollectorUtility.CollectAppModulesForRegistry((AppModulesAsset)target);
            }

            EditorGUILayout.HelpBox(
                "Click 'Auto-Collect AppModuleAssets' to find all AppModuleAsset ScriptableObjects in the project and populate them in the modules array.",
                MessageType.Info);
        }
    }
}
#endif

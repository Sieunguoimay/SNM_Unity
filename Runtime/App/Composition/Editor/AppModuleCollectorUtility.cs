using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.App.Composition
{
    public static class AppModuleCollectorUtility
    {
        public static void CollectAppModules()
        {
            var registries = GetAllAppModuleRegistries();

            if (registries.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No AppModulesAsset found in project", "OK");
                return;
            }

            if (registries.Length == 1)
            {
                CollectAppModulesForRegistry(registries[0]);
                return;
            }

            // Show dialog to select which registry to collect into
            var menu = new GenericMenu();
            foreach (var registry in registries)
            {
                menu.AddItem(new GUIContent(registry.name), false, () => CollectAppModulesForRegistry(registry));
            }
            menu.ShowAsContext();
        }

        public static void CollectAppModulesForRegistry(AppModulesAsset registry)
        {
            var guids = AssetDatabase.FindAssets("t:AppModuleAsset");
            var modules = new AppModuleAsset[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                modules[i] = AssetDatabase.LoadAssetAtPath<AppModuleAsset>(path);
            }

            registry.modules = modules;
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"✓ Collected {modules.Length} AppModuleAsset(s) into '{registry.name}'", registry);
        }

        private static AppModulesAsset[] GetAllAppModuleRegistries()
        {
            var guids = AssetDatabase.FindAssets("t:AppModulesAsset");
            var registries = new AppModulesAsset[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                registries[i] = AssetDatabase.LoadAssetAtPath<AppModulesAsset>(path);
            }

            return registries;
        }
    }
}

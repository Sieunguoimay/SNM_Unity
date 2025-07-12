#if UNITY_EDITOR
using System.Linq;
using Snm.GrabAndToss.ViewSystem;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Snm.Framework.System
{

    public partial class SystemStructureAsset
    {
        [SerializeField] private Object selectedFolder;

        public Object SelectedFolder => selectedFolder;

        public void SetElementAssets(StructureElementAsset[] value)
        {
            elementAssets = value;
        }

        [ContextMenu("Test Build Structure")]
        private void TestBuildStructure()
        {
            using var structure = SystemStructureBuilder.BuildStructure(
                systemAsset: this,
                resolver: new SimpleDependencyResolver(new() {
                    { typeof(IViewSpawnService), new ViewSpawnService_Mock() }
                }));

            var lifecycle = structure as IStructureElementLifecycle;
            lifecycle.Initialize();
            lifecycle.Setup();
            lifecycle.Teardown();
            lifecycle.Cleanup();
        }

        [ContextMenu("Refill All Structures")]
        private void RefillAll()
        {
            RefillAllStructureAssets();
        }

        public static void RefillAllStructureAssets()
        {
            foreach (var structure in GetAllStructureAssets().OrderByDescending(GetStructureAssetDepth))
            {
                Debug.Log("Refilling " + structure.name, structure);
                structure.CollectAllAssetsInFolder();
            }
        }

        public static IEnumerable<SystemStructureAsset> GetAllStructureAssets()
        {
            return AssetDatabase.FindAssets($"t:{nameof(SystemStructureAsset)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SystemStructureAsset>);
        }

        private static int GetStructureAssetDepth(SystemStructureAsset structureAsset)
        {
            return AssetDatabase.GetAssetPath(structureAsset.SelectedFolder).Split('/').Length;
        }

        [ContextMenu("CollectAllAssetsInFolders")]
        public void CollectAllAssetsInFolder()
        {
            var folderPaths = new string[] { AssetDatabase.GetAssetPath(selectedFolder) };
            var otherStructures = AssetDatabase.FindAssets($"t:{nameof(SystemStructureAsset)}", folderPaths)
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<SystemStructureAsset>()
                .Where(s => s != this)
                .ToArray();
            elementAssets = AssetDatabase.FindAssets($"t:{nameof(StructureElementAsset)}", folderPaths)
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<StructureElementAsset>()
                .Where(e => !otherStructures.Any(s => s.ElementAssets.Contains(e)))
                .ToArray();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public bool IsElementOfThisStructure(StructureElementAsset elementAsset)
        {
            return elementAsset != null && elementAssets.Contains(elementAsset);
        }
    }
}
#endif

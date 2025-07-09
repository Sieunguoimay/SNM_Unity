#if UNITY_EDITOR
using System.Linq;
using Snm.GrabAndToss.ViewSystem;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Snm.SystemStructureFramework
{

    public partial class SystemStructureAsset
    {
#if UNITY_EDITOR
        [SerializeField] private Object[] selectedFolders;
#endif
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

        [ContextMenu("CollectAllAssetsInFolder")]
        private void CollectAllAssetsInFolder()
        {
            var folderPaths = selectedFolders.Select(f => AssetDatabase.GetAssetPath(f)).ToArray();
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

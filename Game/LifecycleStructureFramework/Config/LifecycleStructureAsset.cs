using System;
using System.Linq;
using GrabAndToss.Infrastructure;
using Snm.GrabAndToss.ViewSystem;


#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{
   
    public class LifecycleStructureAsset : ScriptableObject
    {
        [SerializeField] private LifecycleUnitAsset[] unitAssets;
#if UNITY_EDITOR
        [SerializeField] private UnityEngine.Object[] selectedFolders;
#endif

        public LifecycleUnitAsset[] UnitAssets => unitAssets;

#if UNITY_EDITOR
        [ContextMenu("Test Build Structure")]
        private void TestBuildStructure()
        {
            using var structure = LifecycleStructureBuilder.BuildStructure(
                systemAsset: this,
                resolver: new SimpleDependencyResolver(new() {
                    { typeof(IViewSpawnService), new ViewSpawnService_Mock() }
                }));
        }

        [ContextMenu("CollectAllAssetsInFolder")]
        private void CollectAllAssetsInFolder()
        {
            unitAssets = selectedFolders
                .Select(f => AssetDatabase.GetAssetPath(f))
                .SelectMany(folder => AssetDatabase.FindAssets($"t:{nameof(LifecycleUnitAsset)}", new[] { folder }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<LifecycleUnitAsset>()
                .ToArray();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}
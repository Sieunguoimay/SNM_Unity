using System.Linq;
using GrabAndToss.Infrastructure;

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
        [SerializeField] private Object selectedFolder;
#endif

        public LifecycleUnitAsset[] UnitAssets => unitAssets;

#if UNITY_EDITOR
        [ContextMenu("Test Build Structure")]
        private void TestBuildStructure()
        {
            using var structure = LifecycleStructureBuilder.BuildStructure(
                systemAsset: this,
                resolver: new SimpleDependencyResolver(new() {
                    { typeof(IViewFactory), new ViewFactory_Empty() }
                }));
        }

        [ContextMenu("CollectAllAssetsInFolder")]
        private void CollectAllAssetsInFolder()
        {
            var folder = AssetDatabase.GetAssetPath(selectedFolder);
            unitAssets = AssetDatabase.FindAssets($"t:{nameof(LifecycleUnitAsset)}", new[] { folder })
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
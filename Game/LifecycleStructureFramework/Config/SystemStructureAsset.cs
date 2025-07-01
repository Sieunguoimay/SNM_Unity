using System.Linq;
using Snm.GrabAndToss.ViewSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Snm.SystemStructureFramework
{
   
    public class SystemStructureAsset : ScriptableObject
    {
        [FormerlySerializedAs("unitAssets")]
        [SerializeField] private StructureElementAsset[] elementDefinitionAssets;
#if UNITY_EDITOR
        [SerializeField] private Object[] selectedFolders;
#endif

        public StructureElementAsset[] ElementDefinitionAssets => elementDefinitionAssets;

#if UNITY_EDITOR
        [ContextMenu("Test Build Structure")]
        private void TestBuildStructure()
        {
            using var structure = SystemStructureBuilder.BuildStructure(
                systemAsset: this,
                resolver: new SimpleDependencyResolver(new() {
                    { typeof(IViewSpawnService), new ViewSpawnService_Mock() }
                }));
        }

        [ContextMenu("CollectAllAssetsInFolder")]
        private void CollectAllAssetsInFolder()
        {
            elementDefinitionAssets = selectedFolders
                .Select(f => AssetDatabase.GetAssetPath(f))
                .SelectMany(folder => AssetDatabase.FindAssets($"t:{nameof(StructureElementAsset)}", new[] { folder }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<StructureElementAsset>()
                .ToArray();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}
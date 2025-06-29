using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace Snm.SystemStructureFramework
{

    [Serializable]
    public class StructureElementReferenceEntry : IStructureElementReference
    {
        [SerializeField] private string injectId;
        [FormerlySerializedAs("asset")]
        [SerializeField] private StructureElementDefinitionAsset definitionAsset;

#if UNITY_EDITOR
        private readonly Type editor_TargetType;
        private readonly Type editor_SelectedForType;
#endif
        string IStructureElementReference.InjectId => injectId;
        IStructureElementDefinition IStructureElementReference.Definition => definitionAsset;

        public StructureElementDefinitionAsset DefinitionAsset => definitionAsset;
        public string InjectId => injectId;

#if UNITY_EDITOR
        public Type Editor_TargetType => editor_TargetType;
        public Type Editor_SelectedForType => editor_SelectedForType;
#endif
        public StructureElementReferenceEntry() { }

#if UNITY_EDITOR
        public StructureElementReferenceEntry(string injectId, StructureElementDefinitionAsset asset, Type targetType)
            : this(injectId, asset)
        {
            editor_TargetType = targetType;
            if (asset != null)
            {
                editor_SelectedForType = asset.GetType().GetCustomAttribute<StructureElementAssetForAttribute>()?.LifecycleUnitType;
            }
        }
#endif

        public StructureElementReferenceEntry(string injectId, StructureElementDefinitionAsset asset)
        {
            this.injectId = injectId;
            this.definitionAsset = asset;
        }

        public void SetAsset(StructureElementDefinitionAsset asset)
        {
            this.definitionAsset = asset;
        }
    }
}
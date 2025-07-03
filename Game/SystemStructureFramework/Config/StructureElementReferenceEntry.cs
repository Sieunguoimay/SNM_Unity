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
        [FormerlySerializedAs("definitionAsset")]
        [SerializeField] private StructureElementAsset referenceAsset;

#if UNITY_EDITOR
        private Type _editor_TargetType;
        private readonly Type editor_SelectedForType;
#endif
        string IStructureElementReference.InjectId => injectId;
        IStructureElementDefinition IStructureElementReference.ReferenceAsset => referenceAsset;

        public StructureElementAsset DefinitionAsset => referenceAsset;
        public string InjectId => injectId;

        public event Action<StructureElementReferenceEntry> OnDefinitionAssetChanged;

#if UNITY_EDITOR
        public Type Editor_TargetType => _editor_TargetType;
        public Type Editor_SelectedForType => editor_SelectedForType;
#endif
        public StructureElementReferenceEntry() { }

#if UNITY_EDITOR
        public void SetTargetType(Type targetType)
        {
            _editor_TargetType = targetType;
        }
#endif

        public StructureElementReferenceEntry(string injectId, StructureElementAsset referenceAsset)
        {
            this.injectId = injectId;
            this.referenceAsset = referenceAsset;
        }

        public void SetAsset(StructureElementAsset asset)
        {
            referenceAsset = asset;
            OnDefinitionAssetChanged?.Invoke(this);
        }
    }
}
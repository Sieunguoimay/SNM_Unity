using System;
using System.Reflection;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{

    [Serializable]
    public class LifecycleUnitReferenceEntry : ILifecycleUnitReference
    {
        [SerializeField] private string injectId;
        [SerializeField] private LifecycleUnitAsset asset;

#if UNITY_EDITOR
        private readonly Type editor_TargetType;
        private readonly Type editor_SelectedForType;
#endif
        string ILifecycleUnitReference.InjectId => injectId;
        ILifecycleUnitDefinition ILifecycleUnitReference.Asset => asset;

        public LifecycleUnitAsset Asset => asset;
        public string InjectId => injectId;

#if UNITY_EDITOR
        public Type Editor_TargetType => editor_TargetType;
        public Type Editor_SelectedForType => editor_SelectedForType;
#endif
        public LifecycleUnitReferenceEntry() { }

#if UNITY_EDITOR
        public LifecycleUnitReferenceEntry(string injectId, LifecycleUnitAsset asset, Type targetType)
            : this(injectId, asset)
        {
            editor_TargetType = targetType;
            if (asset != null)
            {
                editor_SelectedForType = asset.GetType().GetCustomAttribute<LifecycleUnitAssetForAttribute>()?.LifecycleUnitType;
            }
        }
#endif

        public LifecycleUnitReferenceEntry(string injectId, LifecycleUnitAsset asset)
        {
            this.injectId = injectId;
            this.asset = asset;
        }

        public void SetAsset(LifecycleUnitAsset asset)
        {
            this.asset = asset;
        }
    }
}
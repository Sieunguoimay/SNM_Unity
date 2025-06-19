using System;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{

    [Serializable]
    public class LifecycleUnitReferenceEntry: ILifecycleUnitReference
    {
        [SerializeField] private string injectId;
        [SerializeField] private LifecycleUnitAsset asset;

        string ILifecycleUnitReference.InjectId => injectId;
        ILifecycleUnitDefinition ILifecycleUnitReference.Asset => asset;

        public LifecycleUnitAsset Asset => asset;
        public string InjectId => injectId;

        public LifecycleUnitReferenceEntry() { }

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
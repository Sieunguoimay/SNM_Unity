using System.Collections.Generic;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{

    public abstract class LifecycleUnitAsset : ScriptableObject, ILifecycleUnitDefinition
    {
        [HideInInspector]
        [SerializeField] private LifecycleUnitReferenceEntry[] unitReferences;

        IReadOnlyList<ILifecycleUnitReference> ILifecycleUnitDefinition.UnitReferences => unitReferences;

        public IReadOnlyList<LifecycleUnitReferenceEntry> UnitReferences => unitReferences;

        public void SetUnitReferences(LifecycleUnitReferenceEntry[] referenceEntries)
        {
            unitReferences = referenceEntries;
        }

        public ILifecycleUnit CreateLifecycleUnit()
        {
            return CreateLifecyleUnit_Override();
        }

        protected abstract ILifecycleUnit CreateLifecyleUnit_Override();
    }
}
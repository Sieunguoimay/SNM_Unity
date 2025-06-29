using System.Collections.Generic;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{

    public abstract class LifecycleUnitAsset : ScriptableObject, ILifecycleUnitDefinition
    {
        [SerializeField] private LifecycleUnitReferenceEntry[] unitReferences = new LifecycleUnitReferenceEntry[0];

        IReadOnlyList<ILifecycleUnitReference> ILifecycleUnitDefinition.UnitReferences => unitReferences;

        public IReadOnlyList<LifecycleUnitReferenceEntry> UnitReferences => unitReferences;

        public void SetUnitReferences(LifecycleUnitReferenceEntry[] referenceEntries)
        {
            unitReferences = referenceEntries;
        }

        public ILifecycleUnit CreateLifecycleUnit(IDepedencyResolver resolver)
        {
            return CreateLifecyleUnit_Override(resolver);
        }

        protected abstract ILifecycleUnit CreateLifecyleUnit_Override(IDepedencyResolver resolver);
    }
}
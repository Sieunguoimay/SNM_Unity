using System.Collections.Generic;
using UnityEngine;

namespace Snm.SystemStructureFramework
{

    public abstract class StructureElementDefinitionAsset : ScriptableObject, IStructureElementDefinition
    {
        [SerializeField] private StructureElementReferenceEntry[] unitReferences = new StructureElementReferenceEntry[0];

        IReadOnlyList<IStructureElementReference> IStructureElementDefinition.UnitReferences => unitReferences;

        public IReadOnlyList<StructureElementReferenceEntry> UnitReferences => unitReferences;

        public void SetUnitReferences(StructureElementReferenceEntry[] referenceEntries)
        {
            unitReferences = referenceEntries;
        }

        public IStructureElement CreateLifecycleUnit(IDepedencyResolver resolver)
        {
            return CreateLifecyleUnit_Override(resolver);
        }

        protected abstract IStructureElement CreateLifecyleUnit_Override(IDepedencyResolver resolver);
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Snm.Framework.System
{

    public abstract class StructureElementAsset : ScriptableObject, IStructureElementDefinition
    {
        [FormerlySerializedAs("unitReferences")]
        [SerializeField] private StructureElementReferenceEntry[] elementReferences = new StructureElementReferenceEntry[0];

        IReadOnlyList<IStructureElementReference> IStructureElementDefinition.ElementReferences => elementReferences;

#if UNITY_EDITOR
        public IReadOnlyList<StructureElementReferenceEntry> Editor_ElementReferences => elementReferences;

        public void Editor_SetElementReferences(StructureElementReferenceEntry[] referenceEntries)
        {
            elementReferences = referenceEntries;
        }
#endif

        IStructureElement IStructureElementDefinition.CreateElement(IDepedencyResolver resolver)
        {
            return CreateElement_Internal(resolver);
        }

        protected abstract IStructureElement CreateElement_Internal(IDepedencyResolver resolver);
    }
}
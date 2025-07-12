using System;
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

        public event Action<StructureElementAsset> OnValidated;


        private void OnValidate()
        {
            OnValidated?.Invoke(this);
        }

        public void Editor_SetElementReferences(StructureElementReferenceEntry[] referenceEntries)
        {
            elementReferences = referenceEntries;
            OnValidated?.Invoke(this);
        }
#endif

        IStructureElement IStructureElementDefinition.CreateLifecycleUnit(IDepedencyResolver resolver)
        {
            return CreateLifecyleUnit_Override(resolver);
        }

        protected abstract IStructureElement CreateLifecyleUnit_Override(IDepedencyResolver resolver);
    }
}
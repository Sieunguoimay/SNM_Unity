using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Snm.SystemStructureFramework
{
    public static class SystemStructureBuilder
    {
        public static SystemStructure BuildStructure(SystemStructureAsset systemAsset, IDepedencyResolver resolver)
        {
            var dictionary = new Dictionary<IStructureElementDefinition, IStructureElement>();
            var definitions = systemAsset.ElementAssets;

            foreach (var definition in definitions)
            {
                var element = ((IStructureElementDefinition)definition).CreateLifecycleUnit(resolver);
                dictionary.Add(definition, element);
            }

            foreach (IStructureElementDefinition definition in definitions)
            {
                var unit = dictionary[definition];
                var unitType = unit.GetType();
                var references = definition.ElementReferences;
                foreach (var reference in references)
                {
                    if (dictionary.ContainsKey(reference.ReferenceAsset))
                    {
                        var referenceElement = dictionary[reference.ReferenceAsset];
                        var fieldName = reference.InjectId;
                        InjectDependencies(unit, referenceElement, unitType, fieldName);
                    }
                    else
                    {
                        Debug.LogError($"Failed to find Reference {reference.ReferenceAsset} for {definition}", definition as UnityEngine.Object);
                    }
                }
            }

            return new SystemStructure(dictionary);
        }

        private static void InjectDependencies(IStructureElement element, IStructureElement reference, Type elementType, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var type = elementType;

            while (type != null)
            {
                var fieldInfo = type.GetField(fieldName, flags);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(element, reference);
                    break;
                }
                else
                {
                    type = type.BaseType;
                }
            }
        }
    }
}
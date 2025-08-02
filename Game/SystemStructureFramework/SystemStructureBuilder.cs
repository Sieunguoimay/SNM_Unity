using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Snm.Framework.System
{
    public static class SystemStructureBuilder
    {
        public static SystemStructure BuildStructure(SystemStructureAsset systemAsset, IDepedencyResolver resolver)
        {
            return BuildStructure(systemAsset.ElementAssets, resolver);
        }
 
        public static SystemStructure BuildStructure(IEnumerable<IStructureElementDefinition> definitions, IDepedencyResolver resolver)
        {
            var dictionary = new Dictionary<IStructureElementDefinition, IStructureElement>();

            foreach (var definition in definitions)
            {
                var element = definition.CreateElement(resolver);
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
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{
    public static class LifecycleStructureBuilder
    {
        public static LifecycleStructure BuildStructure(LifecycleStructureAsset systemAsset, IDepedencyResolver resolver)
        {
            return new LifecycleStructure(BuildStructureToDictionary(systemAsset.UnitAssets, resolver));
        }

        public static Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> BuildStructureToDictionary(
            ILifecycleUnitDefinition[] assets,
            IDepedencyResolver resolver)
        {
            var dictionary = new Dictionary<ILifecycleUnitDefinition, ILifecycleUnit>();

            foreach (var asset in assets)
            {
                var element = asset.CreateLifecycleUnit(resolver);
                dictionary.Add(asset, element);
            }

            foreach (var asset in assets)
            {
                var unit = dictionary[asset];
                var unitType = unit.GetType();
                var references = asset.UnitReferences;
                foreach (var reference in references)
                {
                    if (dictionary.ContainsKey(reference.Asset))
                    {
                        var referenceElement = dictionary[reference.Asset];
                        var fieldName = reference.InjectId;
                        InjectDependencies(unit, referenceElement, unitType, fieldName);
                    }
                    else
                    {
                        Debug.LogError($"Failed to find Reference {reference.Asset} for {asset}", asset as UnityEngine.Object);
                    }
                }
            }

            return dictionary;
        }

        private static void InjectDependencies(ILifecycleUnit element, ILifecycleUnit reference, Type elementType, string fieldName)
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
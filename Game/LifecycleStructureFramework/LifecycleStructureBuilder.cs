using System;
using System.Collections.Generic;
using System.Reflection;

namespace Snm.LifecycleStructureFramework
{
    public static class LifecycleStructureBuilder
    {
        public static LifecycleStructure BuildStructure(LifecycleStructureAsset systemAsset)
        {
            return new LifecycleStructure(BuildStructureToDictionary(systemAsset.UnitAssets));
        }

        public static Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> BuildStructureToDictionary(ILifecycleUnitDefinition[] assets)
        {
            var dictionary = new Dictionary<ILifecycleUnitDefinition, ILifecycleUnit>();

            foreach (var asset in assets)
            {
                var element = asset.CreateLifecycleUnit();
                dictionary.Add(asset, element);
            }

            foreach (var asset in assets)
            {
                var unit = dictionary[asset];
                var unitType = unit.GetType();
                var references = asset.UnitReferences;
                foreach (var reference in references)
                {
                    var referenceElement = dictionary[reference.Asset];
                    var fieldName = reference.InjectId;
                    InjectDependencies(unit, referenceElement, unitType, fieldName);
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
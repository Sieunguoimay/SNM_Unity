#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Snm.SystemStructureFramework
{
    [CustomEditor(typeof(StructureElementDefinitionAsset), true)]
    public class StructureElementDefinitionAsset_Editor : Editor
    {
        private void OnEnable()
        {
            var unitAsset = (StructureElementDefinitionAsset)target;
            var att = target.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();

            if (att != null && att.LifecycleUnitType != null)
            {
                UpdateReferenceEntries(unitAsset, att.LifecycleUnitType);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.LabelField("Lifecycle Units", EditorStyles.boldLabel);

            var unitAsset = (StructureElementDefinitionAsset)target;

            var anyChanged = false;

            foreach (var reference in unitAsset.UnitReferences)
            {
                anyChanged |= DrawElementReferenceEntry(reference);
            }

            if (anyChanged)
            {
                EditorUtility.SetDirty(unitAsset);
            }
        }

        private bool DrawElementReferenceEntry(StructureElementReferenceEntry entry)
        {
            var result = false;

            var isValidRuntimeType = entry.Editor_TargetType != null;
            var isValidAssetType = entry.DefinitionAsset != null && entry.Editor_TargetType.IsAssignableFrom(entry.Editor_SelectedForType);
            var isValid = isValidAssetType && isValidRuntimeType;

            var errorText = "";
            if (!isValidRuntimeType)
            {
                errorText = "(Invalid Runtime Type)";
            }
            else if (!isValidAssetType)
            {
                errorText = "(Invalid Asset Type)";
            }

            var color = GUI.color;
            GUI.color = isValid ? color : Color.red;
            var newAsset = (StructureElementDefinitionAsset)EditorGUILayout.ObjectField(FormatFieldName(entry.InjectId) + errorText, entry.DefinitionAsset, typeof(StructureElementDefinitionAsset), false);
            if (newAsset != entry.DefinitionAsset)
            {
                entry.SetAsset(newAsset);
                result = true;
            }
            GUI.color = color;
            return result;
        }

        private void UpdateReferenceEntries(StructureElementDefinitionAsset unitAsset, Type unitType)
        {
            var fields = GetReferenceFields(unitType);

            var newEntries = fields
                .Select(field => new StructureElementReferenceEntry(
                    injectId: field.Name,
                    asset: unitAsset.UnitReferences.FirstOrDefault(ef => ef.InjectId == field.Name)?.DefinitionAsset,
                    targetType: field.FieldType))
                .ToArray();

            unitAsset.SetUnitReferences(newEntries);
        }

        private static IEnumerable<FieldInfo> GetReferenceFields(Type type)
        {
            var currentType = type;

            while (currentType != null)
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<StructureElementReferenceAttribute>() != null)
                    {
                        yield return field;
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        public static string FormatFieldName(string fieldName)
        {
            var formatted = Regex.Replace(fieldName, @"([a-z])([A-Z])", "$1 $2");

            formatted = char.ToUpper(formatted[0]) + formatted[1..];

            return formatted;
        }
    }
}
#endif
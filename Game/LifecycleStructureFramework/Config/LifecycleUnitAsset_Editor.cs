#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{
    [CustomEditor(typeof(LifecycleUnitAsset), true)]
    public class LifecycleUnitAsset_Editor : Editor
    {
        private void OnEnable()
        {
            var unitAsset = (LifecycleUnitAsset)target;
            var att = target.GetType().GetCustomAttribute<LifecycleUnitAssetForAttribute>();

            if (att != null && att.LifecycleUnitType != null)
            {
                UpdateReferenceEntries(unitAsset, att.LifecycleUnitType);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.LabelField("Lifecycle Units", EditorStyles.boldLabel);

            var unitAsset = (LifecycleUnitAsset)target;

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

        private bool DrawElementReferenceEntry(LifecycleUnitReferenceEntry entry)
        {
            var result = false;

            var isValidRuntimeType = entry.Editor_TargetType != null;
            var isValidAssetType = entry.Asset != null && entry.Editor_TargetType.IsAssignableFrom(entry.Editor_SelectedForType);
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
            var newAsset = (LifecycleUnitAsset)EditorGUILayout.ObjectField(FormatFieldName(entry.InjectId) + errorText, entry.Asset, typeof(LifecycleUnitAsset), false);
            if (newAsset != entry.Asset)
            {
                entry.SetAsset(newAsset);
                result = true;
            }
            GUI.color = color;
            return result;
        }

        private void UpdateReferenceEntries(LifecycleUnitAsset unitAsset, Type unitType)
        {
            var fields = GetReferenceFields(unitType);
            var newEntries = fields
                .Select(field => new LifecycleUnitReferenceEntry(
                    injectId: field.Name,
                    asset: unitAsset.UnitReferences.FirstOrDefault(ef => ef.InjectId == field.Name)?.Asset,
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
                    if (field.GetCustomAttribute<UnitReferenceAttribute>() != null)
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
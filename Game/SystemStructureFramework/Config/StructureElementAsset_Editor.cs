#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.SystemStructureFramework
{
    [CustomEditor(typeof(StructureElementAsset), true)]
    public class StructureElementAsset_Editor : Editor
    {
        /*        private void OnEnable()
                {
                    var unitAsset = (StructureElementAsset)target;
                    var att = target.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();

                    if (att != null && att.LifecycleUnitType != null)
                    {
                        UpdateReferenceEntries(unitAsset, att.LifecycleUnitType);
                    }
                }
        */
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var defaultInspector = base.CreateInspectorGUI();
            root.Add(defaultInspector);
            return root;
        }
        /*
                public override void OnInspectorGUI()
                {
                    DrawDefaultInspector();

                    EditorGUILayout.LabelField("Lifecycle Units", EditorStyles.boldLabel);

                    var unitAsset = (StructureElementAsset)target;

                    var anyChanged = false;

                    foreach (var reference in unitAsset.Editor_ElementReferences)
                    {
                        anyChanged |= DrawElementReferenceEntry(reference);
                    }

                    if (anyChanged)
                    {
                        EditorUtility.SetDirty(unitAsset);
                    }
                }
        */
        private bool DrawElementReferenceEntry(StructureElementReferenceEntry entry)
        {
            var result = false;

            var isValidRuntimeType = entry.Editor_TargetType != null;
            var isValidAssetType = entry.DefinitionAsset != null
                && entry.Editor_SelectedForType != null
                && entry.Editor_TargetType.IsAssignableFrom(entry.Editor_SelectedForType)
                || entry.Editor_SelectedForType == null;
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
            var newAsset = (StructureElementAsset)EditorGUILayout.ObjectField(FormatFieldName(entry.InjectId) + errorText, entry.DefinitionAsset, typeof(StructureElementAsset), false);
            if (newAsset != entry.DefinitionAsset)
            {
                entry.SetAsset(newAsset);
                result = true;
            }
            GUI.color = color;
            return result;
        }

        private void UpdateReferenceEntries(StructureElementAsset unitAsset, Type unitType)
        {
            var fields = GetReferenceFields(unitType);

            var newEntries = fields
                .Select(field => new StructureElementReferenceEntry(
                    injectId: field.Name,
                    asset: unitAsset.Editor_ElementReferences.FirstOrDefault(ef => ef.InjectId == field.Name)?.DefinitionAsset,
                    targetType: field.FieldType))
                .ToArray();

            unitAsset.Editor_SetElementReferences(newEntries);
        }

        private static IEnumerable<FieldInfo> GetReferenceFields(Type type)
        {
            var currentType = type;

            while (currentType != null)
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<ElementReferenceAttribute>() != null)
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
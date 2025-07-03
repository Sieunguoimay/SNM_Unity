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
        private StructureElementAssetVE _containerVE;

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

        private void OnEnable()
        {
            UpdateReferenceEntries();

            Debug.Log("OnEnable");
        }

        private void OnDisable()
        {
            Debug.Log($"OnDisable {_containerVE}");

            if (_containerVE != null)
            {
                _containerVE.Dispose();
                _containerVE = null;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            Debug.Log("CreateInspectorGUI");
            var elementAsset = (StructureElementAsset)target;
            var root = new VisualElement();
            var defaultInspector = CreateEditor(target);
            root.Add(new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                defaultInspector.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("IMGUIContainer ChangeCheck");
                    _containerVE.RefreshVE();
                }
            }));
            root.Add(_containerVE = new StructureElementAssetVE(elementAsset));
            return root;
        }

        private void UpdateReferenceEntries()
        {
            var referenceFields = GetReferenceFields().ToArray();
            var elementAsset = (StructureElementAsset)target;
            var existingEntries = elementAsset.Editor_ElementReferences;

            var newEntries = referenceFields
                .Select(f =>
                {
                    var entry = existingEntries.FirstOrDefault(e => e.InjectId == f.Name);
                    if (entry != null)
                    {
                        return entry;
                    }
                    return new StructureElementReferenceEntry(f.Name, null, f.FieldType);
                })
                .ToArray();

            elementAsset.Editor_SetElementReferences(newEntries);

            EditorUtility.SetDirty(elementAsset);
        }

        private IEnumerable<FieldInfo> GetReferenceFields()
        {
            var att = target.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();
            if (att != null)
            {
                return GetReferenceFields(att.ElementType);
            }
            return Enumerable.Empty<FieldInfo>();
        }

        private class StructureElementAssetVE : Foldout, IDisposable
        {
            private readonly StructureElementAsset elementAsset;
            private IReadOnlyList<StructureElementReferenceEntry> _cachedReferenceList;

            public StructureElementAssetVE(StructureElementAsset elementAsset)
            {
                text = "Element References";
                Debug.Log($"{nameof(StructureElementAsset)} Created");
                this.elementAsset = elementAsset;
                UpdateReferenceList();
            }

            public void Dispose()
            {
                foreach (var eve in Children().OfType<StructureElementReferenceEntryVE>())
                {
                    eve.Dispose();
                }

                if (_cachedReferenceList != null)
                {
                    foreach (var r in _cachedReferenceList)
                    {
                        r.OnDefinitionAssetChanged -= AnyReference_OnDefinitionAssetChanged;
                    }
                    _cachedReferenceList = null;
                }

                Debug.Log($"{nameof(StructureElementAsset)} Disposed");
            }

            public void RefreshVE()
            {
                UpdateReferenceList();
            }

            private void UpdateReferenceList()
            {
                foreach (var eve in Children().OfType<StructureElementReferenceEntryVE>())
                {
                    eve.Dispose();
                }
                Clear();

                if (_cachedReferenceList != null)
                {
                    foreach (var r in _cachedReferenceList)
                    {
                        r.OnDefinitionAssetChanged -= AnyReference_OnDefinitionAssetChanged;
                    }
                }

                _cachedReferenceList = elementAsset.Editor_ElementReferences;

                if (_cachedReferenceList != null)
                {
                    foreach (var r in _cachedReferenceList)
                    {
                        r.OnDefinitionAssetChanged += AnyReference_OnDefinitionAssetChanged;
                    }

                    foreach (var er in _cachedReferenceList)
                    {
                        Add(new StructureElementReferenceEntryVE(er));
                    }
                }
            }

            private void AnyReference_OnDefinitionAssetChanged(StructureElementReferenceEntry entry)
            {
                SaveElementAsset();
            }

            private void SaveElementAsset()
            {
                EditorUtility.SetDirty(elementAsset);
                Debug.Log($"Saved {elementAsset.name}", elementAsset);
            }
        }

        private class StructureElementReferenceEntryVE : ObjectField, IDisposable
        {
            private readonly StructureElementReferenceEntry entry;
            private StructureElementAssetForAttribute _att;

            public StructureElementReferenceEntryVE(StructureElementReferenceEntry entry)
            {
                this.entry = entry;
                label = entry.InjectId;
                value = entry.DefinitionAsset;
                objectType = typeof(StructureElementAsset);
                this.RegisterValueChangedCallback(ThisObjectField_OnValueChanged);
                UpdateAttribute();
            }

            public void Dispose()
            {
                this.UnregisterValueChangedCallback(ThisObjectField_OnValueChanged);
            }

            private void ThisObjectField_OnValueChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                if (evt.newValue is StructureElementAsset asset)
                {
                    entry.SetAsset(asset);
                }
                UpdateAttribute();
            }

            private void UpdateAttribute()
            {
                if (value != null)
                {
                    _att = value.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();
                }
                else
                {
                    _att = null;
                }
                Validate();
            }

            private void Validate()
            {
                var isValid = false;
                if (_att != null)
                {
                    if (entry.Editor_TargetType != null && entry.Editor_TargetType.IsAssignableFrom(_att.ElementType))
                    {
                        isValid = true;
                    }
                }
                else
                {
                    isValid = true;
                }

                style.color = isValid ? Color.green : Color.red;
            }
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
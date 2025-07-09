#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Snm.Tools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Framework.System
{
    public class StructureElementReferenceEntryVE : VisualElement, IDisposable
    {
        private readonly StructureElementReferenceEntry entry;
        private readonly ObjectField objectField;
        private readonly Label label;
        private StructureElementAssetForAttribute _att;

        public StructureElementReferenceEntryVE(StructureElementReferenceEntry entry)
        {
            this.entry = entry;
            style.flexDirection = FlexDirection.Row;
            style.width = new StyleLength(Length.Percent(100));

            Add(label = new Label()
            {
                text = FormatFieldName(entry.InjectId) + $" ({entry.Editor_TargetType.Name})",
                style = { width = new StyleLength(Length.Percent(40)), alignSelf = Align.Center }
            });
            Add(objectField = new ObjectField()
            {
                name = "Object Field",
                value = entry.DefinitionAsset,
                objectType = typeof(StructureElementAsset),
                style = { flexGrow = 1, flexShrink = 1, minWidth = 0 }
            });

            Add(new Button(SelectObject) { text = "...", style = { width = 25 } });

            objectField.RegisterValueChangedCallback(ObjectField_OnValueChanged);
            UpdateAttribute();
        }

        public void Dispose()
        {
            objectField.UnregisterValueChangedCallback(ObjectField_OnValueChanged);
        }

        private void ObjectField_OnValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.newValue is StructureElementAsset asset)
            {
                entry.SetAsset(asset);
            }
            UpdateAttribute();
        }

        private void UpdateAttribute()
        {
            if (objectField.value != null)
            {
                _att = objectField.value.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();
            }
            else
            {
                _att = null;
            }
            UpdateValidateColor();
        }

        private void UpdateValidateColor()
        {
            var isValid = false;
            if (_att == null)
            {
                if (objectField.value != null)
                {
                    isValid = true;
                }
            }
            else
            {
                if (entry.Editor_TargetType.IsAssignableFrom(_att.ElementType))
                {
                    isValid = true;
                }
            }

            label.style.color = isValid ? Color.green : Color.red;
        }

        private void SelectObject()
        {
            var options = GetOptions().ToArray();
            ObjectPickerWindow.Show(options, obj => objectField.value = obj);
        }

        public IEnumerable<StructureElementAsset> GetOptions()
        {
            return AssetDatabase.FindAssets($"t:{nameof(StructureElementAsset)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StructureElementAsset>);
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
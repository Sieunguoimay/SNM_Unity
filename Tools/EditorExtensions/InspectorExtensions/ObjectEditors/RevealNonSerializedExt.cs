#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace InspectorExtensions
{

    public class RevealNonSerializedExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Attribute;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Bottom;
        int IInspectorExtension.Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target) => target is RevealNonSerializedAttribute;

        void IInspectorExtension.CleanUpStaticData()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            extensionElement.style.display = DisplayStyle.Flex;
            extensionElement.style.flexDirection = FlexDirection.Row;

            var label = new Label() { text = "#", tooltip = "Reveal NonSerialized" };
            label.style.marginLeft = 9;
            label.style.marginTop = 2;
            label.style.paddingRight = 1;
            extensionElement.Add(label);

            var ve = CreateVE((extensionElement as InspectorExtensionElement_MemberInfo).MemberInfo, extensionElement.Target);
            ve.style.flexGrow = 1;
            ve.SetEnabled(false);
            extensionElement.Add(ve);
        }

        private VisualElement CreateVE(MemberInfo mi, object target)
        {
            var fieldInfo = mi as FieldInfo;
            var propertyInfo = mi as PropertyInfo;

            if (propertyInfo == null && fieldInfo == null) return new VisualElement();

            var memberType = propertyInfo?.PropertyType ?? fieldInfo?.FieldType;
            var value = fieldInfo?.GetValue(target) ?? propertyInfo?.GetValue(target);

            if (value is IEnumerable objects)
            {
                var visualElement = new VisualElement();
                visualElement.Add(new Label(mi.Name));
                var i = 0;
                foreach (var o in objects)
                {
                    visualElement.Add(new FieldVE($"{mi.Name}[{i++}]", "", o));
                }
                return visualElement;
            }

            return new FieldVE(mi.Name, memberType.Name, value);
        }

        private class FieldVE : VisualElement
        {
            public FieldVE(string label, string memberType, object value)
            {
                Add(GetFieldVE(FormatString(label), memberType, value));
            }

            private VisualElement GetFieldVE(string label, string memberType, object value)
            {
                if (value is string str) { return new TextField(label) { value = str }; }
                if (value is float f) { return new FloatField(label) { value = f }; }
                if (value is double d) { return new DoubleField(label) { value = d }; }
                if (value is int i) { return new IntegerField(label) { value = i }; }
                if (value is bool b) { return new Toggle(label) { value = b }; }
                if (value is UnityEngine.Object o) { return new ObjectField(label) { value = o }; }
                return new Label($"{label} of type {memberType} is Unsupported or Null");
            }

            private static string FormatString(string input)
            {
                var words = Regex.Split(input, @"(?<!^)(?=[A-Z])");
                var formattedString = string.Join(" ", words);
                return formattedString;
            }
        }
    }
}

#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class TextAboveAttribute : PropertyAttribute
    {
        public string TextData { get; }
        public bool IsDynamic { get; }

        public TextAboveAttribute(string textData, bool isDynamic = false)
        {
            TextData = textData;
            IsDynamic = isDynamic;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TextAboveAttribute))]
    public class TextAboveDrawer : PropertyDrawer
    {
        private readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var textLines = GetTextLines(property);

            position.height -= (EditorGUIUtility.singleLineHeight + 2) * textLines.Length;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label);

            foreach (var textLine in textLines)
            {
                position.y += position.height;
                position.height = EditorGUIUtility.singleLineHeight + 2;
                EditorGUI.LabelField(position, textLine);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var textLines = GetTextLines(property);

            return EditorGUI.GetPropertyHeight(property) + (EditorGUIUtility.singleLineHeight + 2) * textLines.Length;
        }

        private string[] GetTextLines(SerializedProperty property)
        {
            var att = attribute as TextAboveAttribute;
            if (att.IsDynamic)
            {
                var target = property.serializedObject.targetObject;
                var propertyInfo = target.GetType().GetProperty(att.TextData, flags);
                if (propertyInfo == null)
                {
                    return Array.Empty<string>();
                }
                return (propertyInfo.GetValue(target) as IEnumerable<string>).ToArray();
            }
            else
            {
                return att.TextData.Split('\n');
            }
        }
    }
#endif

}
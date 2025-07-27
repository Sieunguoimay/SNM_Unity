using Sieunguoimay.Reflection;
using UnityEngine;
using System.Collections.Generic;
using Snm.Tools;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Snm.PropertyAttributes
{
    public class DisableIfAttribute : PropertyAttribute
    {
        public readonly string PropertyName;
        public readonly bool Value;

        public DisableIfAttribute(string propertyName, bool value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfAttributeDrawer : PropertyDrawer
    {
        private bool _shouldDisable;
        private readonly Dictionary<string, object> objects = new();
        private DisableIfAttribute _att;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as DisableIfAttribute;
            if (!objects.ContainsKey(property.propertyPath))
            {
                objects.Add(property.propertyPath, SerializeUtility.GetObjectToWhichPropertyBelong(property));
            }
            _shouldDisable = (bool)ReflectionUtility.GetDataFromMember(objects[property.propertyPath], _att.PropertyName, false) == _att.Value;

            EditorGUI.BeginProperty(position, label, property);
            var ge = GUI.enabled;
            GUI.enabled = !_shouldDisable;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = ge;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }

    }
#endif
}
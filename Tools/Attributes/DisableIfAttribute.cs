using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

#if UNITY_EDITOR
using Sieunguoimay.Reflection;
using Snm.Tools;
using UnityEditor;
#endif

namespace Snm.PropertyAttributes
{
    public class DisableIfAttribute : PropertyAttribute
    {
        public readonly string PropertyName;
        public readonly object Value;

        public DisableIfAttribute(string propertyName, object value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }
#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfAttributeDrawer : PropertyDrawer
    {
        // Unity reuses one PropertyDrawer instance across sibling SerializedProperties (e.g. array
        // elements), so per-instance state must be keyed by propertyPath or siblings will stomp it.
        private readonly Dictionary<string, bool> _shouldDisableByPath = new();
        private DisableIfAttribute _att;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as DisableIfAttribute;

            var obj = SerializeUtility.GetObjectToWhichPropertyBelong(property);
            var path = property.propertyPath;
            var shouldDisable = _shouldDisableByPath.TryGetValue(path, out var prev) && prev;

            if (obj is not Array and not IList and not IEnumerable)
            {
                var current = obj != null ? ReflectionUtility.GetDataFromMember(obj, _att.PropertyName, false) : null;
                // Equals (static) handles nulls on either side without NRE.
                shouldDisable = Equals(current, _att.Value);
                _shouldDisableByPath[path] = shouldDisable;
            }

            EditorGUI.BeginProperty(position, label, property);
            var ge = GUI.enabled;
            GUI.enabled = !shouldDisable;
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
using Sieunguoimay.Reflection;
using UnityEngine;
using System.Collections.Generic;
using Snm.Tools;
using System;
using System.Collections;


#if UNITY_EDITOR
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
        private bool _shouldDisable;
        private DisableIfAttribute _att;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as DisableIfAttribute;

            var obj = SerializeUtility.GetObjectToWhichPropertyBelong(property);

            if (obj is not Array and not IList and not IEnumerable)
            {
                _shouldDisable = obj != null && ReflectionUtility.GetDataFromMember(obj, _att.PropertyName, false).Equals(_att.Value);
            }

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
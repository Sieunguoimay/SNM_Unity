using System;
using Snm.Tools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Snm.PropertyAttributes
{
    public class SerializeTypeAttribute : PropertyAttribute
    {
        public bool Editable { get; }

        public SerializeTypeAttribute(bool editable = false)
        {
            Editable = editable;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SerializeTypeAttribute))]
    public class PropertyDrawer_SerializeTypeAttribute : PropertyDrawer
    {
        private string _cachedStr;
        private MonoScript _monoScript;
        private SerializeTypeAttribute _att;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as SerializeTypeAttribute;

            var enabled = GUI.enabled;

            EditorGUI.BeginProperty(position, label, property);

            var objectFieldRect = new Rect(position)
            {
                width = position.width / 3
            };
            position.width -= objectFieldRect.width;
            objectFieldRect.x += position.width;

            if (_cachedStr != property.stringValue)
            {
                _cachedStr = property.stringValue;
                _monoScript = MonoScriptFinder.GetMonoScriptForType(Type.GetType(property.stringValue));
            }

            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);

            GUI.enabled = _att.Editable ? enabled : false;
            var newObj = EditorGUI.ObjectField(objectFieldRect, _monoScript, typeof(MonoScript), false);
            if (_att.Editable && _monoScript != newObj)
            {
                _monoScript = newObj as MonoScript;
                property.stringValue = _monoScript.GetClass().AssemblyQualifiedName;
            }
            GUI.enabled = enabled;

            EditorGUI.EndProperty();
        }
    }
#endif
}
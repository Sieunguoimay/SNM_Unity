using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class PropertyGUIAttribute : PropertyAttribute
    {
        public string MethodName { get; private set; }
        public bool IsPropertyInRootObject { get; private set; }

        public PropertyGUIAttribute(string methodName, bool isPropertyInRootObject = false)
        {
            MethodName = methodName;
            IsPropertyInRootObject = isPropertyInRootObject;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PropertyGUIAttribute))]
    public class PropertyGUIDrawer : PropertyDrawer
    {
        // Unity reuses one drawer instance across sibling properties — key state by propertyPath.
        // MethodInfo can be shared per-Type (not per-path) since siblings of an array share a type.
        private PropertyGUIAttribute _att;
        private readonly Dictionary<string, object> _targetByPath = new();
        private readonly Dictionary<Type, MethodInfo> _methodByType = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            _att ??= attribute as PropertyGUIAttribute;
            var path = property.propertyPath;
            if (!_targetByPath.TryGetValue(path, out var target))
            {
                target = _att.IsPropertyInRootObject
                    ? property.serializedObject.targetObject
                    : SerializeUtility.GetObjectToWhichPropertyBelong(property);
                _targetByPath[path] = target;
            }
            if (target == null) return;

            var targetType = target.GetType();
            if (!_methodByType.TryGetValue(targetType, out var methodInfo))
            {
                var t = targetType;
                while (t != null)
                {
                    methodInfo = t.GetMethod(_att.MethodName, SerializeUtility.Flag);
                    if (methodInfo != null) break;
                    t = t.BaseType;
                }
                _methodByType[targetType] = methodInfo;
            }
            methodInfo?.Invoke(target, null);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }

    }
#endif
}
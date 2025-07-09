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
        private PropertyGUIAttribute _att;
        private MethodInfo _methodInfo;
        private object _target;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            _att ??= attribute as PropertyGUIAttribute;
            _target ??= _att.IsPropertyInRootObject ? property.serializedObject.targetObject : SerializeUtility.GetDirectTargetObject(property);
            if (_methodInfo == null)
            {
                var t = _target.GetType();
                while (t != null)
                {
                    _methodInfo = t.GetMethod(_att.MethodName, SerializeUtility.Flag);
                    if (_methodInfo == null)
                    {
                        t = t.BaseType;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            _methodInfo?.Invoke(_target, null);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }

    }
}
#endif
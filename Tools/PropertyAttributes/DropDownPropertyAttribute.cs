using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class DropDownPropertyAttribute : PropertyAttribute
    {
        public DropDownPropertyAttribute(string displayTextGetMethod, string valuesGetMethod, bool isPropertyInRootObject = false)
        {
            DisplayTextGetMethod = displayTextGetMethod;
            ValuesProperty = valuesGetMethod;
            IsPropertyInRootObject = isPropertyInRootObject;
        }

        public string DisplayTextGetMethod { get; }
        public string ValuesProperty { get; }
        public bool IsPropertyInRootObject { get; internal set; }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DropDownPropertyAttribute))]
    public class DropDownPropertyDrawer : PropertyDrawer
    {
        private static readonly BindingFlags flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private DropDownPropertyAttribute _att;
        private MethodInfo _displayTextGetMethod;
        private IEnumerable<string> _values;
        private object _target;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_att == null)
            {
                _att = attribute as DropDownPropertyAttribute;

                _target = _att.IsPropertyInRootObject ? property.serializedObject.targetObject : StringSelectorDrawer.GetDirectTargetObject(property);
                _displayTextGetMethod = _target.GetType().GetMethod(_att.DisplayTextGetMethod, flag);
                _values = _target.GetType().GetProperty(_att.ValuesProperty, flag).GetValue(_target) as IEnumerable<string>;
            }


            EditorGUI.BeginProperty(position, label, property);
            if (EditorGUI.DropdownButton(position,
                new GUIContent(GetDisplaytext(property.stringValue)),
                FocusType.Passive))
            {
                var gm = new GenericMenu();
                foreach (var v in _values)
                {
                    gm.AddItem(new GUIContent(GetDisplaytext(v)), property.stringValue == v, () =>
                    {

                    });
                }
                gm.ShowAsContext();
            }
            EditorGUI.EndProperty();
        }

        private string GetDisplaytext(string value)
        {
            return _displayTextGetMethod != null
                ? _displayTextGetMethod.Invoke(_target, new object[] { value }) as string
                : value;
        }
    }
#endif
}
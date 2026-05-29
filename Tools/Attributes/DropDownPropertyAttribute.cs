using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

        // Unity reuses one drawer instance across sibling properties; key state by propertyPath.
        private sealed class State
        {
            public MethodInfo DisplayTextGetMethod;
            public IEnumerable<string> Values;
            public object Target;
        }
        private readonly Dictionary<string, State> _stateByPath = new();
        private DropDownPropertyAttribute _att;

        private State GetState(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_stateByPath.TryGetValue(path, out var s))
            {
                _att ??= attribute as DropDownPropertyAttribute;
                s = new State
                {
                    Target = _att.IsPropertyInRootObject
                        ? property.serializedObject.targetObject
                        : StringSelectorDrawer.GetDirectTargetObject(property),
                };
                if (s.Target != null)
                {
                    var t = s.Target.GetType();
                    s.DisplayTextGetMethod = t.GetMethod(_att.DisplayTextGetMethod, flag);
                    s.Values = t.GetProperty(_att.ValuesProperty, flag)?.GetValue(s.Target) as IEnumerable<string>;
                }
                _stateByPath[path] = s;
            }
            return s;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var s = GetState(property);

            EditorGUI.BeginProperty(position, label, property);
            if (EditorGUI.DropdownButton(position,
                new GUIContent(GetDisplaytext(s, property.stringValue)),
                FocusType.Passive))
            {
                var gm = new GenericMenu();
                if (s.Values != null)
                {
                    foreach (var v in s.Values)
                    {
                        gm.AddItem(new GUIContent(GetDisplaytext(s, v)), property.stringValue == v, () =>
                        {

                        });
                    }
                }
                gm.ShowAsContext();
            }
            EditorGUI.EndProperty();
        }

        private string GetDisplaytext(State s, string value)
        {
            return s.DisplayTextGetMethod != null
                ? s.DisplayTextGetMethod.Invoke(s.Target, new object[] { value }) as string
                : value;
        }
    }
#endif
}
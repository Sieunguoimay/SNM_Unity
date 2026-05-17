using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class StringSelectorAttribute : PropertyAttribute
    {
        public string MemberName { get; private set; }
        public bool IsPropertyInRootObject { get; private set; }
        public bool ShouldCacheStrings { get; private set; }
        public bool ShouldDrawLabel { get; private set; }
        public string MaskFunction { get; }

        public StringSelectorAttribute(string memberName,
            bool isPropertyInRootObject = false,
            bool shouldCacheStrings = false,
            bool shouldDrawLabel = true,
            string maskFunction = "")
        {
            MemberName = memberName;
            IsPropertyInRootObject = isPropertyInRootObject;
            ShouldCacheStrings = shouldCacheStrings;
            ShouldDrawLabel = shouldDrawLabel;
            MaskFunction = maskFunction;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(StringSelectorAttribute))]
    public class StringSelectorDrawer : PropertyDrawer
    {
        private static readonly BindingFlags flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        // Unity reuses one drawer instance across sibling properties (e.g. array elements);
        // per-instance fields would be stomped. Key all per-property state by propertyPath.
        private sealed class State
        {
            public string[] Strings;
            public bool Valid;
            public bool FirstTime = true;
            public object Target;
            public MethodInfo MaskFunction;
        }
        private readonly Dictionary<string, State> _stateByPath = new();
        private StringSelectorAttribute _att;

        private State GetState(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_stateByPath.TryGetValue(path, out var s))
            {
                _att ??= attribute as StringSelectorAttribute;
                s = new State();
                s.Target = _att.IsPropertyInRootObject ? property.serializedObject.targetObject : GetDirectTargetObject(property);
                s.MaskFunction = s.Target?.GetType().GetMethod(_att.MaskFunction, flag);
                _stateByPath[path] = s;
            }
            return s;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as StringSelectorAttribute;
            var s = GetState(property);

            EditorGUI.BeginProperty(position, label, property);

            if (_att.ShouldDrawLabel)
            {
                EditorGUI.PrefixLabel(position, label);
                position.x += EditorGUIUtility.labelWidth;
                position.width -= EditorGUIUtility.labelWidth;
            }

            if (s.FirstTime)
            {
                s.Strings = GetStrings(property, s).ToArray();
                s.Valid = s.Strings.Contains(property.stringValue);
            }

            var show = false;

            var color = GUI.color;
            GUI.color = s.Valid ? color : Color.red;
            position.width -= 25;


            if (EditorGUI.DropdownButton(position, new GUIContent(Mask(s, property.stringValue)), FocusType.Passive))
            {
                if (s.Strings.Length < 20)
                {
                    ShowGenericMenu(s, s.Strings, property.stringValue, newValue =>
                        {
                            property.serializedObject.Update();
                            property.stringValue = newValue;
                            property.serializedObject.ApplyModifiedProperties();
                            s.Valid = s.Strings.Contains(property.stringValue);
                        });
                }
                else
                {
                    show = true;
                }
            }

            position.x += position.width;
            position.width = 25;
            if (GUI.Button(position, "...")) show = true;
            GUI.color = color;
            EditorGUI.EndProperty();

            if (show)
            {
                SearchWindow.Show(s.Strings, result =>
            {
                property.serializedObject.Update();
                property.stringValue = result;
                property.serializedObject.ApplyModifiedProperties();
                s.Valid = s.Strings.Contains(property.stringValue);
            });
            }
            s.FirstTime = false;
        }

        private string Mask(State s, string value)
        {
            return s.MaskFunction != null
                ? s.MaskFunction.Invoke(s.Target, new object[] { value }) as string
                : value;
        }

        private IEnumerable<string> GetStrings(SerializedProperty property, State s)
        {
            var att = attribute as StringSelectorAttribute;
            var shouldCreateNewStrings = !att.ShouldCacheStrings || (att.ShouldCacheStrings && s.Strings == null);

            if (shouldCreateNewStrings)
            {
                try
                {
                    var target = att.IsPropertyInRootObject ? property.serializedObject.targetObject : GetDirectTargetObject(property);
                    var member = target.GetType().GetMember(att.MemberName, flag)[0];
                    var value = member switch
                    {
                        PropertyInfo pi => pi.GetValue(target),
                        FieldInfo fi => fi.GetValue(target),
                        MethodInfo mi => mi.Invoke(target, new object[] { }),
                        _ => throw new System.NotImplementedException(),
                    };
                    var strings = (value as IEnumerable<string>) ?? Enumerable.Empty<string>();

                    s.Strings = strings.ToArray();
                }
                catch (Exception)
                {
                    s.Strings = Array.Empty<string>();
                }
            }

            return s.Strings;
        }

        public static object GetDirectTargetObject(SerializedProperty property)
        {
            var pathComponents = property.propertyPath.Replace("Array.data[", "[").Split('.');
            var pathComponentsToDirectObject = pathComponents.Take(pathComponents.Length - 1);

            object currentObject = property.serializedObject.targetObject;

            var pattern = @"\[(\d+)\]";
            var regex = new Regex(pattern);
            foreach (var p in pathComponentsToDirectObject)
            {
                if (p.StartsWith('['))
                {
                    if (int.TryParse(regex.Match(p).Groups[1].Value, out int arrIndex))
                    {
                        // Use IList so List<T> and T[] both work; original cast to object[] failed for List<T>.
                        if (currentObject is IList list)
                            currentObject = list[arrIndex];
                        else
                            return null;
                    }
                }
                else
                {
                    var t = currentObject.GetType();
                    while (t != null)
                    {
                        var fieldInfo = t.GetField(p, flag);
                        if (fieldInfo != null)
                        {
                            currentObject = fieldInfo.GetValue(currentObject);
                            break;
                        }
                        else
                        {
                            t = t.BaseType;
                        }
                    }
                }
            }

            return currentObject;
        }

        private void ShowGenericMenu(State s, IEnumerable<string> strings, string currentValue, System.Action<string> onNewValue)
        {
            var menu = new GenericMenu();
            foreach (var i in strings)
            {
                menu.AddItem(new GUIContent(Mask(s, i)), i == currentValue, () =>
                {
                    onNewValue?.Invoke(i);
                });
            }
            menu.ShowAsContext();
        }
    }

#endif

}
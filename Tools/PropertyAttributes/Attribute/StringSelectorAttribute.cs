using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sieunguoimay.Reflection;
#if UNITY_EDITOR
using Sieunguoimay.Serialization;
using UnityEditor;
#endif
using UnityEngine;
using WilliamExtra;

namespace Sieunguoimay.Attribute
{
    public class BaseSelectorAttribute : PropertyAttribute
    {
        private readonly string _providerVariableName;
        private readonly bool _isProviderPropertyInBase;
        private readonly string _callbackToModifySelectedValue;
        private readonly int pathBackStep = 0;

        protected BaseSelectorAttribute(int pathBackStep, string name, string callbackToModifySelectedValue)
            : this(name, false, callbackToModifySelectedValue)
        {
            this.pathBackStep = pathBackStep;
        }

        protected BaseSelectorAttribute(string name, bool isProviderPropertyInBase, string callbackToModifySelectedValue)
        {
            _providerVariableName = name;
            _isProviderPropertyInBase = isProviderPropertyInBase;
            _callbackToModifySelectedValue = callbackToModifySelectedValue;
        }

#if UNITY_EDITOR
        public object GetData(SerializedProperty property)
        {
            object providerObject;

            if (_isProviderPropertyInBase)
            {
                providerObject = property.serializedObject.targetObject;
            }
            else
            {
                if (pathBackStep == 0)
                {
                    providerObject = SerializeUtility.GetObjectToWhichPropertyBelong(property);
                }
                else
                {
                    var objectSequence = SerializeUtility.GetObjectHierarchy(property);

                    if (pathBackStep + 2 < objectSequence.Count)
                    {
                        providerObject = objectSequence[^(pathBackStep + 2)];
                    }
                    else
                    {
                        Debug.LogError($"Failed to GetData! object at pathBackStep={pathBackStep} not exist!");
                        return Enumerable.Empty<object>();
                    }
                }
            }
            return ReflectionUtility.GetDataFromMember(providerObject, _providerVariableName, false);
        }

        public object InvokeCallback(SerializedProperty property, object value)
        {
            if (string.IsNullOrEmpty(_callbackToModifySelectedValue)) return value;
            var providerObject = SerializeUtility.GetObjectToWhichPropertyBelong(property);
            return providerObject.GetType().GetMethod(_callbackToModifySelectedValue)?.Invoke(providerObject, new[] { value });
        }
#endif

        // public void InvokeCallback(SerializedProperty property)
        // {
        //     if (string.IsNullOrEmpty(_callbackMethod)) return;
        //     var providerObject = _isCallbackInBase
        //         ? property.serializedObject.targetObject
        //         : ReflectionUtility.GetObjectToWhichPropertyBelong(property);
        //     ReflectionUtility.GetMethodInfo(providerObject.GetType(), _callbackMethod, false).Invoke(providerObject, null);
        // }
    }

    public class StringSelectorAttribute : BaseSelectorAttribute
    {
        public StringSelectorAttribute(int backStep, string name, string callbackToModifySelectedValue = "")
            : base(backStep, name, callbackToModifySelectedValue)
        {
        }

        public StringSelectorAttribute(string name, bool isProviderPropertyInBase = false, string callbackToModifySelectedValue = "")
            : base(name, isProviderPropertyInBase, callbackToModifySelectedValue)
        {
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(StringSelectorAttribute))]
    public class StringSelectorDrawer : PropertyDrawer
    {
        private GenericMenu _menu;
        private StringSelectorAttribute _att;
        private bool _isValid;
        private SerializedProperty _property;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= (attribute as StringSelectorAttribute);

            if (_property != property)
            {
                _property = property;
                ValidateCurrentValue(property);
            }
            Draw(position, property, label, EditorStyles.label);
        }

        private void Draw(Rect position, SerializedProperty property, GUIContent label, GUIStyle style)
        {
            position = EditorGUI.PrefixLabel(position, label, style);
            if (!CreateMenuWithStringProperty(position, property, _att)) return;
            _menu?.ShowAsContext();
        }
        private void ValidateCurrentValue(SerializedProperty property)
        {
            var ids = GetIds(property, _att);
            _isValid = ids.Any(id => IsActive(property, id));
        }

        private bool CreateMenuWithStringProperty(Rect position, SerializedProperty property,
            StringSelectorAttribute objectSelector)
        {
            var threeDotsWidth = 25;
            position.width -= threeDotsWidth;
            var openWindow = DrawDropdownButton(position, property);
            position.x += position.width;
            position.width = threeDotsWidth;
            var openPicker = GUI.Button(position, "...");
            if (openPicker)
            {
                PropertyValuePickerWindow.PickString(GetIds(property, objectSelector), () => "", str =>
                {
                    ApplySelectedString(property, (string)str);
                });
                return false;
            }
            if (!openWindow)
            {
                _menu = null;
                return false;
            }

            if (_menu != null) return true;
            _menu = new GenericMenu();

            var ids = GetIds(property, objectSelector);

            if (ids == null) return false;

            foreach (var id in ids)
            {
                _menu.AddItem(new GUIContent(id), IsActive(property, id), data =>
                {
                    ApplySelectedString(property, (string)data);
                }, id);
            }

            return true;
        }
        private void ApplySelectedString(SerializedProperty property, string str)
        {
            property.serializedObject.Update();
            OnSelected(property, _att, str);
            property.serializedObject.ApplyModifiedProperties();
            ValidateCurrentValue(property);
        }

        protected virtual bool DrawDropdownButton(Rect position, SerializedProperty property)
        {
            var color = GUI.color;
            GUI.color = _isValid ? color : Color.red;
            var value = EditorGUI.DropdownButton(position, new GUIContent(GetDisplay(property)), FocusType.Keyboard);
            GUI.color = color;
            return value;
        }

        protected virtual string GetDisplay(SerializedProperty property)
        {
            return property.stringValue;
        }

        protected virtual bool IsActive(SerializedProperty property, string item)
        {
            return property.stringValue == item;
        }

        protected virtual void OnSelected(SerializedProperty property, StringSelectorAttribute att, string item)
        {
            property.stringValue = (string)att.InvokeCallback(property, item);
        }

        protected virtual IEnumerable<string> GetIds(SerializedProperty property,
            StringSelectorAttribute objectSelector)
        {
            return objectSelector.GetData(property) as IEnumerable<string>;
        }
    }
#endif
}
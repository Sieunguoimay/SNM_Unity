using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class ObjectSelectorAttribute : PropertyAttribute
    {
        public string ProviderMember { get; }

        public ObjectSelectorAttribute(string providerMember = "")
        {
            ProviderMember = providerMember;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(ObjectSelectorAttribute))]
    public class PropertyDrawer_ObjectSelector : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position.width -= 25;
            EditorGUI.PropertyField(position, property, label, true);

            position.x += position.width;
            position.width = 25;

            if (GUI.Button(position, "..."))
            {
                ShowMenuItem(property);
            }

            EditorGUI.EndProperty();
        }

        private void ShowMenuItem(SerializedProperty property)
        {
            var options = ExtractOptions(property);

            var menu = new GenericMenu();
            foreach (var option in options)
            {
                var localOption = option;
                var isCurrent = option == property.objectReferenceValue;
                menu.AddItem(new GUIContent(option.GetType().Name), isCurrent, () =>
                {
                    property.objectReferenceValue = localOption;
                });
            }
            menu.ShowAsContext();
        }

        private UnityEngine.Object[] ExtractOptions(SerializedProperty property)
        {
            var directObject = SerializeUtility.GetObjectToWhichPropertyBelong(property);

            if (directObject == null) return Array.Empty<UnityEngine.Object>();

            var att = attribute as ObjectSelectorAttribute;
            if (string.IsNullOrEmpty(att.ProviderMember))
            {
                return GetAssociatedObjects(property.objectReferenceValue).ToArray();
            }

            var member = directObject.GetType().GetMember(att.ProviderMember).FirstOrDefault();
            object result = null;
            if (member is MethodInfo methodInfo)
            {
                result = methodInfo.Invoke(directObject, null);
            }
            else if (member is PropertyInfo propInfo)
            {
                result = propInfo.GetValue(directObject);
            }

            return result as UnityEngine.Object[];
        }

        private IEnumerable<UnityEngine.Object> GetAssociatedObjects(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                foreach (var o in go.GetComponentsInChildren<Component>()
                    .OfType<UnityEngine.Object>())
                {
                    yield return o;
                }
            }
            yield return obj;
        }
    }
#endif
}
#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class ObjectSelectorAttribute : PropertyAttribute
    {
        public string ProviderMember { get; }

        public ObjectSelectorAttribute(string providerMember)
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
            var directObject = SerializeUtility.GetDirectTargetObject(property);
            if (directObject != null)
            {
                var array = GetOptions(directObject);
                ObjectPickerWindow.Show(array, obj =>
                {
                    Debug.Log("Selected Object: " + obj.name, obj);
                    property.objectReferenceValue = obj;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
        }

        private Object[] GetOptions(object directObject)
        {
            var att = attribute as ObjectSelectorAttribute;
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

            return result as Object[];
        }
    }
#endif
}
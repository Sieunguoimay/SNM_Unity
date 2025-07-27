using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Sieunguoimay.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeReferenceSupportAttribute : PropertyAttribute
    {
        private readonly Type[] candidateTypes;

        public SerializeReferenceSupportAttribute(params Type[] candidateTypes)
        {
            this.candidateTypes = candidateTypes;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerializeReferenceSupportAttribute))]
        private class ThisPropertyDrawer : PropertyDrawer
        {
            private SerializeReferenceSupportAttribute _att;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                _att = attribute as SerializeReferenceSupportAttribute;

                var data = property.boxedValue;
                var value = new GUIContent(data?.GetType()?.Name ?? "NULL");
                PropertyFieldWithDropdown(position, property, label, value);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            private void ShowOptionsContextMenu(SerializedProperty property)
            {
                var data = property.boxedValue;
                var current = data?.GetType()?.Name ?? "NULL";

                var menu = new GenericMenu();

                foreach (var type in GetCandiateTypes())
                {
                    menu.AddItem(new GUIContent(type.Name), current == type.Name, () =>
                    {
                        AssignReference(property, type);
                    });
                }

                menu.ShowAsContext();
            }

            private IEnumerable<Type> GetCandiateTypes()
            {
                return GetSerializableImplementations(GetFieldType());
            }

            private void AssignReference(SerializedProperty property, Type type)
            {
                Undo.RecordObject(property.serializedObject.targetObject, "SerializeReferenceSupportAttribute.AssignReference");

                property.boxedValue = property.boxedValue != null ? CopySerialize(property.boxedValue, type) : CreateObjectByType(type);

                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            private static object CopySerialize(object from, Type targetType)
            {
                var json = JsonConvert.SerializeObject(from);
                return JsonConvert.DeserializeObject(json, targetType);
            }

            private static object CreateObjectByType(Type type)
            {
                return Activator.CreateInstance(type);
            }

            private void PropertyFieldWithDropdown(Rect position, SerializedProperty property, GUIContent label, GUIContent value)
            {
                EditorGUI.BeginProperty(position, label, property);

                var dropdownRect = new Rect(position.x + position.width * 2f / 5f, position.y, position.width * 3f / 5f, EditorGUIUtility.singleLineHeight);

                if (GUI.Button(dropdownRect, value, EditorStyles.popup))
                {
                    ShowOptionsContextMenu(property);
                }

                EditorGUI.PropertyField(position, property, label, true);

                EditorGUI.EndProperty();
            }

            private List<Type> GetSerializableImplementations(Type interfaceType)
            {
                var candidateTypes = _att.candidateTypes != null && _att.candidateTypes.Length > 0
                    ? _att.candidateTypes
                    : AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        // Ignore dynamic or non-user assemblies if needed
                        try { return assembly.GetTypes(); } catch { return new Type[0]; }
                    });
                return candidateTypes
                    .Where(type =>
                        interfaceType.IsAssignableFrom(type) &&
                        type.IsClass &&
                        !type.IsAbstract &&
                        !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                        type.GetCustomAttribute<SerializableAttribute>() != null)
                    .ToList();
            }


            private Type GetFieldType()
            {
                if (fieldInfo.FieldType.IsArray)
                {
                    return fieldInfo.FieldType.GetElementType();
                }
                else
                {
                    return fieldInfo.FieldType;
                }
            }
        }
#endif
    }
}
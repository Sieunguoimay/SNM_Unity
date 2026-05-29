using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using PropertyAttribute = UnityEngine.PropertyAttribute;

namespace Snm.PropertyAttributes
{
    /// <summary>
    /// Unique across ScriptableObjects. Supports types: String, Integer, Float, Double, Enum, ObjectReference
    /// </summary>
    public class UniqueFieldAttribute : PropertyAttribute
    {
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UniqueFieldAttribute))]
    public class UniqueFieldPropertyDrawer : PropertyDrawer
    {
        // Unity reuses one drawer instance across sibling properties — key state by propertyPath.
        // We also hold the SerializedObjects to dispose them in the finalizer (PropertyDrawer has
        // no Dispose hook); leaking these accumulates editor memory across selections.
        private readonly Dictionary<string, SerializedProperty[]> _propsByPath = new();
        private readonly Dictionary<string, SerializedObject[]> _ownedSerializedObjectsByPath = new();

        ~UniqueFieldPropertyDrawer()
        {
            foreach (var arr in _ownedSerializedObjectsByPath.Values)
                foreach (var so in arr)
                    so?.Dispose();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var globalEnabled = GUI.color;
            GUI.color = IsUniqueFieldAcrossAssets(property) ? globalEnabled : Color.red;
            EditorGUI.PropertyField(position, property, label);
            GUI.color = globalEnabled;
        }
        private bool IsUniqueFieldAcrossAssets(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (!_propsByPath.TryGetValue(path, out var props))
            {
                props = GetCorrespondingPropertyOfOtherAssets(property, out var owners);
                _propsByPath[path] = props;
                _ownedSerializedObjectsByPath[path] = owners;
            }

            return CompareAgainstOthers(property, props);
        }
        private bool CompareAgainstOthers(SerializedProperty property, IEnumerable<SerializedProperty> _otherProperties)
        {
            var value = GetValue(property);
            foreach (var sp in _otherProperties)
            {
                var otherValue = GetValue(sp);
                if (value?.Equals(otherValue) ?? false)
                {
                    return false;
                }
            }
            return true;
        }
        private object GetValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                // Use enumValueFlag for [Flags] enums (bitfield), enumValueIndex otherwise.
                SerializedPropertyType.Enum => IsFlagsEnum() ? property.enumValueFlag : property.enumValueIndex,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                _ => throw new System.NotImplementedException(),
            };
        }

        private bool IsFlagsEnum()
        {
            var t = fieldInfo?.FieldType;
            if (t == null) return false;
            if (t.IsArray) t = t.GetElementType();
            else if (t.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition()))
                t = t.GetGenericArguments()[0];
            return t != null && t.IsEnum && t.IsDefined(typeof(FlagsAttribute), false);
        }

        private static SerializedProperty[] GetCorrespondingPropertyOfOtherAssets(SerializedProperty property, out SerializedObject[] owners)
        {
            var objectType = property.serializedObject.targetObject.GetType();
            var ownerList = GetAllObjectsOfType(objectType)
                .Where(o => o != property.serializedObject.targetObject)
                .Select(o => new SerializedObject(o))
                .ToArray();
            owners = ownerList;
            return ownerList.Select(so => so.FindProperty(property.propertyPath)).ToArray();
        }

        private static IEnumerable<UnityEngine.Object> GetAllObjectsOfType(System.Type type)
        {
            return AssetDatabase.FindAssets($"t:{type.Name}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>);
        }
    }
#endif
}
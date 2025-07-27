#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;
using PropertyAttribute = UnityEngine.PropertyAttribute;

namespace Sieunguoimay.Attribute
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
        private SerializedProperty[] _serializedProperties;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var globalEnabled = GUI.color;
            GUI.color = IsUniqueFieldAcrossAssets(property) ? globalEnabled : Color.red;
            EditorGUI.PropertyField(position, property, label);
            GUI.color = globalEnabled;
        }
        private bool IsUniqueFieldAcrossAssets(SerializedProperty property)
        {
            _serializedProperties ??= GetCorrespondingPropertyOfOtherAssets(property);

            return CompareAgainstOthers(property, _serializedProperties);
        }
        private static bool CompareAgainstOthers(SerializedProperty property, IEnumerable<SerializedProperty> _otherProperties)
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
        private static object GetValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                SerializedPropertyType.Enum => property.enumValueFlag,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                _ => throw new System.NotImplementedException(),
            };
        }

        private static SerializedProperty[] GetCorrespondingPropertyOfOtherAssets(SerializedProperty property)
        {
            var objectType = property.serializedObject.targetObject.GetType();
            return GetAllObjectsOfType(objectType)
                .Where(o => o != property.serializedObject.targetObject)
                .Select(o => new SerializedObject(o).FindProperty(property.propertyPath)).ToArray();
        }

        private static IEnumerable<Object> GetAllObjectsOfType(System.Type type)
        {
            return AssetDatabase.FindAssets($"t: {type.Name}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Object>);
        }
    }
#endif
}
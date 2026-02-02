#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public static class SerializeUtility
    {
        public static BindingFlags Flag => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public static object GetObjectToWhichPropertyBelong(SerializedProperty property)
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
                        currentObject = (currentObject as object[])[arrIndex];
                    }
                }
                else
                {
                    var t = currentObject.GetType();
                    while (t != null)
                    {
                        var fieldInfo = t.GetField(p, Flag);
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

        public static IEnumerable<(SerializedProperty, T)> GetPropertiesWithAttribute<T>(
            ScriptableObject so)
            where T : PropertyAttribute
        {
            var serializedObject = new SerializedObject(so);
            var iterator = serializedObject.GetIterator();

            while (iterator.NextVisible(true))
            {
                if (iterator.propertyPath.Contains("Array.size")) continue;

                var fieldInfo = GetFieldInfo(iterator);
                if (fieldInfo != null)
                {
                    var att = fieldInfo.GetCustomAttribute<T>();
                    if (att != null)
                    {
                        yield return (iterator.Copy(), att);
                    }
                }
            }
        }

        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var pathComponents = property.propertyPath.Replace("Array.data[", "[").Split('.');
            var currentObject = (object)property.serializedObject.targetObject;
            FieldInfo fieldInfo = null;

            var pattern = @"\[(\d+)\]";
            var regex = new Regex(pattern);

            foreach (var p in pathComponents)
            {
                if (p.StartsWith('['))
                {
                    if (int.TryParse(regex.Match(p).Groups[1].Value, out int arrIndex))
                    {
                        currentObject = GetElementAtIndex(currentObject, arrIndex);
                    }
                }
                else
                {
                    var t = currentObject.GetType();
                    while (t != null)
                    {
                        var fi = t.GetField(p, Flag);
                        if (fi != null)
                        {
                            currentObject = fi.GetValue(currentObject);
                            fieldInfo = fi;
                            break;
                        }
                        else
                        {
                            t = t.BaseType;
                        }
                    }
                }
            }
            return fieldInfo;
        }

        public static object GetElementAtIndex(object obj, int index)
        {
            if (obj is Array arr)
            {
                return (index >= 0 && index < arr.Length) ? arr.GetValue(index) : null;
            }
            if (obj is IList list)
            {
                return (index >= 0 && index < list.Count) ? list[index] : null;
            }

            var type = obj.GetType();

            // IReadOnlyList<T> / IList<T> via interface reflection
            var indexableIface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                        || i.GetGenericTypeDefinition() == typeof(IList<>)));

            if (indexableIface != null)
            {
                var countProp = indexableIface.GetProperty("Count");
                var indexer = indexableIface.GetProperty("Item");
                if (countProp != null && indexer != null)
                {
                    int count = (int)countProp.GetValue(obj);
                    if (index >= 0 && index < count)
                        return indexer.GetValue(obj, new object[] { index });
                    return null;
                }
            }
            return null;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Snm.Tools.ObjectBrowser
{
    public class ObjectExposedItemsDrawer
    {
        public static void DrawExposedItems(
            IEnumerable<ObjectExposedItem> exposedItems,
            Action<ObjectExposedItem> clickEventHandler,
            bool allowExpose,
            bool hasObject,
            bool displayTypeHash,
            object targetObject = null)
        {
            foreach (var exposedItem in exposedItems)
            {
                EditorGUILayout.BeginHorizontal();

                // Action button (-> or invoke)
                if (exposedItem.MemberInfo is MethodInfo methodInfo)
                {
                    if (methodInfo.GetParameters().Length == 0 && (hasObject || methodInfo.IsStatic))
                    {
                        if (GUILayout.Button(new GUIContent("()", "Invoke Method"), GUILayout.Width(25)))
                        {
                            clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {
                        GUILayout.Space(25);
                    }
                }
                else
                {
                    if (allowExpose)
                    {
                        if (GUILayout.Button(new GUIContent("->", "Go into"), GUILayout.Width(25)))
                        {
                            clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {
                        GUILayout.Space(25);
                    }
                }

                // Member name with tooltip showing type info
                var memberTooltip = GetMemberTooltip(exposedItem);
                var nameContent = new GUIContent(
                    displayTypeHash ? exposedItem.MemberName : exposedItem.DisplayMemberName,
                    memberTooltip);
                EditorGUILayout.LabelField(nameContent);

                // Value display — editable for primitives, read-only otherwise
                if (exposedItem.Value is Object asset)
                {
                    EditorGUILayout.ObjectField(asset, typeof(Object), false);
                }
                else if (targetObject != null && CanEdit(exposedItem))
                {
                    DrawEditableValue(exposedItem, targetObject);
                }
                else
                {
                    var valueTooltip = exposedItem.DisplayValue ?? "null";
                    EditorGUILayout.LabelField(new GUIContent(valueTooltip, valueTooltip));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static string GetMemberTooltip(ObjectExposedItem item)
        {
            if (item.MemberInfo == null)
                return item.DisplayMemberName;

            var typeName = item.MemberInfo switch
            {
                FieldInfo fi => fi.FieldType.Name,
                PropertyInfo pi => pi.PropertyType.Name,
                MethodInfo mi => $"() → {mi.ReturnType.Name}",
                _ => ""
            };

            return $"{item.DisplayMemberName}\nType: {typeName}\nValue: {item.DisplayValue ?? "null"}";
        }

        private static bool CanEdit(ObjectExposedItem item)
        {
            if (item.MemberInfo is not FieldInfo fi) return false;
            if (fi.IsLiteral || fi.IsInitOnly) return false;

            var t = fi.FieldType;
            return t == typeof(int) || t == typeof(float) || t == typeof(double)
                || t == typeof(string) || t == typeof(bool) || t == typeof(long)
                || t.IsEnum;
        }

        private static void DrawEditableValue(ObjectExposedItem item, object target)
        {
            var fi = (FieldInfo)item.MemberInfo;
            var fieldType = fi.FieldType;

            EditorGUI.BeginChangeCheck();

            object newValue = item.Value;

            if (fieldType == typeof(int))
                newValue = EditorGUILayout.IntField((int)(item.Value ?? 0));
            else if (fieldType == typeof(float))
                newValue = EditorGUILayout.FloatField((float)(item.Value ?? 0f));
            else if (fieldType == typeof(double))
                newValue = EditorGUILayout.DoubleField((double)(item.Value ?? 0.0));
            else if (fieldType == typeof(long))
                newValue = EditorGUILayout.LongField((long)(item.Value ?? 0L));
            else if (fieldType == typeof(bool))
                newValue = EditorGUILayout.Toggle((bool)(item.Value ?? false));
            else if (fieldType == typeof(string))
                newValue = EditorGUILayout.TextField((string)(item.Value ?? ""));
            else if (fieldType.IsEnum)
                newValue = EditorGUILayout.EnumPopup((Enum)(item.Value ?? Enum.ToObject(fieldType, 0)));

            if (EditorGUI.EndChangeCheck())
            {
                fi.SetValue(target, newValue);
                item.Value = newValue;
                item.DisplayValue = ObjectReflectionExposer.ValueToString(newValue);
            }
        }
    }
}
#endif

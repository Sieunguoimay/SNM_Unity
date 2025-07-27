#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Sieunguoimay.Tools
{
    public class RuntimeObjectExpose
    {
        private IEnumerable<FieldInfo> _allFields;
        private IEnumerable<PropertyInfo> _allProperties;
        private IEnumerable<MethodInfo> _allMethods;
        private readonly ITargetObjectProvider _objectProvider;

        public RuntimeObjectExpose(ITargetObjectProvider objectProvider)
        {
            _objectProvider = objectProvider;
        }

        public interface ITargetObjectProvider
        {
            object TargetObject { get; }
        }

        public IReadOnlyList<ObjectExposedItem> ExposeObject()
        {
            if (_objectProvider.TargetObject == null) return null;
            if (_allFields == null || _allProperties == null || _allMethods == null)
            {
                UpdateReflectionInfos();
            }

            var exposedItems = new List<ObjectExposedItem>();

            foreach (var fieldInfo in _allFields)
            {
                object value = null;
                try
                {
                    value = fieldInfo.GetValue(_objectProvider.TargetObject);
                }
                catch (Exception)
                {
                    //ignore
                }

                exposedItems.Add(new ObjectExposedItem
                {
                    FieldName = fieldInfo.Name,
                    DisplayValue = value?.ToString(),
                    IsPrimitive = IsPrimitive(fieldInfo.FieldType),
                    Value = value,
                    MemberInfo = fieldInfo
                });
            }

            foreach (var propInfo in _allProperties)
            {
                object value = null;
                try
                {
                    value = propInfo.GetValue(_objectProvider.TargetObject);
                }
                catch (Exception)
                {
                    //ignore
                }

                exposedItems.Add(new ObjectExposedItem
                {
                    FieldName = propInfo.Name,
                    DisplayValue = value?.ToString(),
                    IsPrimitive = IsPrimitive(propInfo.PropertyType),
                    Value = value,
                    MemberInfo = propInfo
                });
            }

            foreach (var methodInfo in _allMethods)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    exposedItems.Add(new ObjectExposedItem
                    {
                        FieldName = methodInfo.Name,
                        DisplayValue = methodInfo.ReturnType.Name,
                        IsPrimitive = false,
                        Value = methodInfo.ReturnType.Name,
                        MemberInfo = methodInfo
                    });
                }
            }

            if (_objectProvider.TargetObject is not Array arr) return exposedItems;
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    var value = arr.GetValue(i);
                    if (value == null) continue;
                    exposedItems.Add(new ObjectExposedItem
                    {
                        FieldName = $"[{i}]",
                        DisplayValue = value.ToString(),
                        IsPrimitive = IsPrimitive(value.GetType()),
                        Value = value,
                        MemberInfo = null
                    });
                }
            }

            return exposedItems;
        }

        public void UpdateReflectionInfos()
        {
            var type = _objectProvider.TargetObject.GetType();

            _allFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
            _allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
            _allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        public class ObjectExposedItem
        {
            public string FieldName;
            public string DisplayValue;
            public object Value;
            public bool IsPrimitive;
            public MemberInfo MemberInfo;
        }
#if UNITY_EDITOR

        public class CommonRuntimeObjectExposeEditor
        {
            private readonly Action<ObjectExposedItem> _clickEventHandler;
            private Vector2 _scrollPos;

            public CommonRuntimeObjectExposeEditor(Action<ObjectExposedItem> clickEventHandler)
            {
                _clickEventHandler = clickEventHandler;
            }

            public void DrawExposedItems(IEnumerable<ObjectExposedItem> exposedItems)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(GUI.skin.box);
                foreach (var exposedItem in exposedItems)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (exposedItem.MemberInfo is MethodInfo methodInfo && methodInfo.GetParameters().Length == 0)
                    {
                        if (GUILayout.Button(new GUIContent($"()", "Invoke Method"), GUILayout.Width(25)))
                        {
                            _clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {

                        var isExposable = !exposedItem.IsPrimitive && exposedItem.Value != null;
                        if (isExposable)
                        {
                            if (GUILayout.Button(new GUIContent($"->", "Go into"), GUILayout.Width(25)))
                            {
                                _clickEventHandler?.Invoke(exposedItem);
                            }
                        }
                    }

                    EditorGUILayout.LabelField($"{exposedItem.FieldName}");
                    if (exposedItem.Value is Object asset)
                    {
                        EditorGUILayout.ObjectField(asset, typeof(Object), false);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{exposedItem.DisplayValue}");
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
            }
        }
#endif
    }
}
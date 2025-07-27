using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sieunguoimay.Attribute;
using Sieunguoimay.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Sieunguoimay.Tools
{
    [Serializable]
    public class UnityObjectPathSelector
    {
        [SerializeField, ComponentSelector] private UnityEngine.Object sourceObject;

#if UNITY_EDITOR
        [PathSelector(nameof(sourceObject))]
#endif
        [SerializeField]
        private string path;

        [field: NonSerialized] public PathExecutor Executor { get; private set; }

        public Type PathFinalType => ReflectionUtility.GetTypeAtPath(sourceObject?.GetType(),
            string.IsNullOrEmpty(path) ? Array.Empty<string>() : path.Split('.'), true);

        public void Setup(bool cache)
        {
            Executor = new PathExecutor();
            Executor.Setup(path, sourceObject, cache);
        }

        public class PathExecutor
        {
            private MemberInfoWrapper[] _memberInfos;
            private object _sourceObject;

            private bool _cache;
            private object _runtimeObject;

            public object CachedRuntimeObject //=> ExecutePath();
            {
                get
                {
                    if (_cache)
                    {
                        return _runtimeObject ??= ExecutePath();
                    }

                    return ExecutePath();
                }
            }

            public void Setup(string path, object sourceObject, bool cache, bool isNameFormatted = true, bool setupAtRuntime = false)
            {
                _sourceObject = sourceObject;
                if (string.IsNullOrEmpty(path))
                {
                    _memberInfos = Array.Empty<MemberInfoWrapper>();
                    return;
                }

                var pathSegments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                _memberInfos = new MemberInfoWrapper[pathSegments.Length];

                var currObj = _sourceObject;
                var currType = _sourceObject.GetType();

                for (var i = 0; i < pathSegments.Length; i++)
                {
                    _memberInfos[i] = new MemberInfoWrapper();
                    _memberInfos[i].Setup(currType, pathSegments[i], isNameFormatted);

                    if (setupAtRuntime)
                    {
                        currObj = _memberInfos[i].GetMemberValue(currObj);
                        currType = currObj?.GetType() ?? _memberInfos[i].GetMemberType();
                    }
                    else
                    {
                        currType = _memberInfos[i].GetMemberType();
                    }
                }

                _cache = cache;
            }

            public object ExecutePath()
            {
                return _memberInfos.Aggregate(_sourceObject, (current, mi) => mi.GetMemberValue(current));
            }

            public Type RuntimePathFinalType =>
                _memberInfos.Length == 0 ? _sourceObject.GetType() : _memberInfos[^1].GetMemberType();
        }

        public class MemberInfoWrapper
        {
            private FieldInfo _fieldInfo;
            private PropertyInfo _propertyInfo;
            private MethodInfo _methodInfo;
            private bool _isArray;
            private Type _arrayElementType;
            private int _arrayIndex;

            public void Setup(Type type, string memberName, bool isNameFormatted)
            {
                if (type == null) return;
                if (type.IsArray || IsGenericList(type) || IsReadOnlyList(type))
                {
                    _isArray = true;
                    _arrayElementType = type.GetElementType();
                    if (int.TryParse(memberName, out var i))
                    {
                        _arrayIndex = i;
                    }
                }

                _fieldInfo = ReflectionUtility.GetFieldInfo(type, memberName, isNameFormatted);
                if (_fieldInfo != null) return;
                _propertyInfo = ReflectionUtility.GetPropertyInfo(type, memberName, isNameFormatted);
                if (_propertyInfo != null) return;
                _methodInfo = ReflectionUtility.GetMethodInfo(type, memberName, isNameFormatted);
            }

            public static bool IsGenericList(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
            }

            public static bool IsReadOnlyList(Type type)
            {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
            }

            public Type GetMemberType()
            {
                if (_isArray) return _arrayElementType;
                if (_fieldInfo != null) return _fieldInfo.FieldType;
                if (_propertyInfo != null) return _propertyInfo.PropertyType;
                if (_methodInfo != null) return _methodInfo.ReturnType;
                return null;
            }

            public object GetMemberValue(object obj)
            {
                if (_isArray) return (obj as Array)?.GetValue(_arrayIndex);
                if (_fieldInfo != null) return _fieldInfo.GetValue(obj);
                if (_propertyInfo != null) return _propertyInfo.GetValue(obj);
                if (_methodInfo != null) return _methodInfo.Invoke(obj, null);
                return null;
            }
        }

        public class CompactAttribute : PropertyAttribute
        {
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UnityObjectPathSelector))]
    public class UnityObjectPathSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height -= 2;
            position.height /= 2;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceObject"));
            position.y += position.height + 2;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("path"));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 2 + 2;
        }
    }

    [CustomPropertyDrawer(typeof(UnityObjectPathSelector.CompactAttribute))]
    public class UnityObjectPathSelectorCompactDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.width /= 2;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("sourceObject"), GUIContent.none);
            position.x += position.width;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("path"), GUIContent.none);
            EditorGUI.EndProperty();
        }
    }
#endif
}
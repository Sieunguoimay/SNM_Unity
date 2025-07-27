using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sieunguoimay.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Sieunguoimay.Attribute
{
    public class PathSelectorAttribute : BaseSelectorAttribute
    {
        public bool IsGetPath { get; }
        public bool IsTypeProvided { get; }

        public PathSelectorAttribute(string objectPropertyName, bool isGetPath = true,
            bool isProviderPropertyInBase = false, bool typeProvided = false)
            : base(objectPropertyName, isProviderPropertyInBase, "")
        {
            IsGetPath = isGetPath;
            IsTypeProvided = typeProvided;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PathSelectorAttribute))]
    public class PathSelectorDrawer : PropertyDrawer
    {
        private GenericMenu _menu;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not PathSelectorAttribute objectSelector) return;

            position = EditorGUI.PrefixLabel(position, label);
            if (property.propertyType == SerializedPropertyType.String)
            {
                if (!CreateMenuWithStringProperty(position, property, objectSelector)) return;
            }

            _menu?.ShowAsContext();
        }

        private bool _changed;

        private bool CreateMenuWithStringProperty(Rect position, SerializedProperty property,
            PathSelectorAttribute objectSelector)
        {
            position.width = 100;

            var path = string.IsNullOrEmpty(property.stringValue) ? Array.Empty<string>() : property.stringValue.Split('.');
            var siblingObject = GetSiblingObject(property, objectSelector);
            var rootType = objectSelector.IsTypeProvided ? siblingObject as Type : siblingObject?.GetType();
            if (rootType == null) return false;
            var open = false;
            for (var i = 0; i < path.Length; i++)
            {
                var p = path[i];

                if (i > 0)
                {
                    rootType = ReflectionUtility.GetReturnTypeOfMember(rootType, path[i - 1], true);
                }

                var openWindow = EditorGUI.DropdownButton(position,
                    new GUIContent(ReflectionUtility.FormatName.Extract(p)), FocusType.Keyboard);
                position.x += 102;

                if (!openWindow) continue;
                if (rootType == null) continue;

                _menu = new GenericMenu();

                var ids = GetIds(rootType, objectSelector.IsGetPath);

                if (ids == null) continue;

                foreach (var id in ids)
                {
                    var i1 = i;
                    _menu.AddItem(new GUIContent(id), property.stringValue == id, data =>
                    {
                        path[i1] = ModifyValue((string) data);
                        property.serializedObject.Update();
                        property.stringValue = string.Join('.', path);
                        property.serializedObject.ApplyModifiedProperties();
                        _changed = true;
                    }, id);
                }

                open = true;
            }

            if (_changed)
            {
                _changed = false;
                GUI.changed = true;
            }

            position.width = 25;

            if (path.Length > 0)
            {
                var remove = GUI.Button(position, "-");
                if (remove)
                {
                    property.serializedObject.Update();
                    property.stringValue = string.Join('.', path.Where((_, index) => index != path.Length - 1));
                    property.serializedObject.ApplyModifiedProperties();
                }

                position.x += 27;
            }

            var add = GUI.Button(position, "+");
            if (add)
            {
                property.serializedObject.Update();
                property.stringValue = string.IsNullOrEmpty(property.stringValue)
                    ? "null"
                    : string.Concat(property.stringValue, ".");
                property.serializedObject.ApplyModifiedProperties();
            }

            return open;
        }

        protected virtual object GetSiblingObject(SerializedProperty property,
            PathSelectorAttribute objectSelector)
        {
            return objectSelector.GetData(property);
        }

        private static IEnumerable<string> GetIds(Type type, bool isGetPath)
        {
            // var result = ReflectionUtility.GetAllMethodsAndInterfaces(type);
            // return result.SelectMany(r => r.Item2.Where(m => m.ReturnType == typeof(void)).Select(mi => $"{r.Item1.Name}/{ReflectionUtility.FormatName.FormatMethodName(mi)}"));
            var types = type.GetInterfaces();
            if (types.Length > 0)
            {
                var interfaces = types.Concat(new[] {type}).ToArray();
                var methods = interfaces.SelectMany(i =>
                    i.GetMethods().Where(WhereMethod)
                        .Select(mi => $"{i.Name}/{ReflectionUtility.FormatName.FormatMethodName(mi)}"));
                var props = interfaces.SelectMany(i =>
                    i.GetProperties().Select(pi => $"{i.Name}/{ReflectionUtility.FormatName.FormatPropertyName(pi)}"));
                var fields = interfaces.SelectMany(i =>
                    i.GetFields().Select(fi => $"{i.Name}/{ReflectionUtility.FormatName.FormatFieldName(fi)}"));
                return Array.Empty<string>().Concat(methods).Concat(props).Concat(fields);
            }
            else
            {
                var methods = type.GetMethods().Where(WhereMethod)
                    .Select(ReflectionUtility.FormatName.FormatMethodName);
                var props = type.GetProperties().Select(ReflectionUtility.FormatName.FormatPropertyName);
                var fields = type.GetFields().Select(ReflectionUtility.FormatName.FormatFieldName);
                return Array.Empty<string>().Concat(methods).Concat(props).Concat(fields);
            }

            bool WhereMethod(MethodInfo m)
            {
                var hasParams = m.GetParameters().Length == 0;
                var hasReturnValue = m.ReturnType != typeof(void);
                var isGetProperty = m.Name.StartsWith("get_");
                return !isGetProperty && (!isGetPath || !hasParams && hasReturnValue);
            }
        }

        private static string ModifyValue(string value)
        {
            return value.Split('/').LastOrDefault();
        }
    }
#endif
}
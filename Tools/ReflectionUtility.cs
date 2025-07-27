using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Sieunguoimay.Reflection
{
    public static class ReflectionUtility
    {
        public static object ExecutePathOfObject(object obj, IEnumerable<string> path, bool isNameFormatted)
        {
            if (obj == null) return null;

            var finalObj = obj;
            foreach (var name in path)
            {
                finalObj = GetDataFromMember(finalObj, name, isNameFormatted);
                if (finalObj == null) return null;
            }

            return finalObj;
        }

        public static Type GetTypeAtPath(Type obj, IEnumerable<string> path, bool isNameFormatted)
        {
            if (obj == null) return null;

            var finalObj = obj;
            foreach (var t in path)
            {
                finalObj = GetReturnTypeOfMember(finalObj, t, isNameFormatted);
                if (finalObj == null) return null;
            }

            return finalObj;
        }

        public static object GetDataFromMember(object source, string name, bool isNameFormatted)
        {
            if (source == null) return null;
            var type = source.GetType();
            var f = GetFieldInfo(type, name, isNameFormatted);
            if (f != null) return f.GetValue(source);
            var p = GetPropertyInfo(type, name, isNameFormatted);
            if (p != null) return p.GetValue(source);
            var mi = GetMethodInfo(type, name, isNameFormatted);
            if (mi != null && mi.GetParameters().Length == 0) return mi.Invoke(source, null);
            return null;
        }

        public static Type GetReturnTypeOfMember(Type type, string name, bool isNameFormatted)
        {
            if (type == null) return null;
            var f = GetFieldInfo(type, name, isNameFormatted);
            if (f != null) return f.FieldType;
            var p = GetPropertyInfo(type, name, isNameFormatted);
            if (p != null) return p.PropertyType;
            var mi = GetMethodInfo(type, name, isNameFormatted);
            if (mi != null) return mi.ReturnType;
            return null;
        }

        public static FieldInfo GetFieldInfo(Type type, string name, bool isNameFormatted)
        {
            FieldInfo found = null;

            var tempType = type;
            while (found == null && tempType != null)
            {
                var fields = GetAllFields(tempType);
                found = fields.FirstOrDefault(
                    m => isNameFormatted ? FormatName.FormatFieldName(m).Equals(name) : m.Name.Equals(name));
                tempType = tempType.BaseType;
            }
            return found;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name, bool isNameFormatted)
        {
            PropertyInfo found = null;
            var tempType = type;
            while (found == null && tempType != null)
            {
                var props = GetAllProperties(type);
                found = props.FirstOrDefault(
                    m =>
                    {
                        if (!isNameFormatted) return m.Name.Equals(name);
                        var a = FormatName.FormatPropertyName(m);
                        return a.Equals(name);

                    });
                tempType = tempType.BaseType;
            }
            return found;
        }

        public static MethodInfo GetMethodInfo(Type type, string name, bool isNameFormatted)
        {
            MethodInfo found = null;
            var tempType = type;
            while (found == null && tempType != null)
            {
                var methods = GetAllMethods(type);
                found = methods.FirstOrDefault(m =>
                    isNameFormatted ? FormatName.FormatMethodName(m).Equals(name) : m.Name.Equals(name));
                tempType = tempType.BaseType;
            }
            return found;
        }

        public static object GetValueOfElement(IEnumerable enumerable, int index)
        {
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }

        public static Type GetPropertyOrFieldType(Type type, string propName)
        {
            var prop = type.GetProperty(propName, PropertyFlags);
            var field = type.GetField(propName, FieldFlags);
            return prop == null ? field?.FieldType : prop.PropertyType;
        }

        public static IEnumerable<Type> GetPropertyOrFieldTypes(Type type)
        {
            var prop = type.GetProperties(PropertyFlags);
            return prop.Length == 0
                ? type.GetFields(FieldFlags)?.Select(f => f.FieldType)
                : prop.Select(f => f.PropertyType);
        }

        public static BindingFlags PropertyFlags =>
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.IgnoreCase;

        public static BindingFlags FieldFlags =>
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.IgnoreCase;

        public static BindingFlags MethodFlags =>
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.IgnoreCase;

        public static IEnumerable<Type> GetAllInterfaces(object obj)
        {
            if (obj is GameObject go)
            {
                return go.GetComponents<Component>().SelectMany(c => { return c.GetType().GetInterfaces().Concat(new[] { c.GetType() }); }).Concat(new[] { go.GetType() });
            }

            return obj.GetType().GetInterfaces().Concat(new[] { obj.GetType() });
        }

        public static IEnumerable<TInfo> GetAllInfos<TInfo>(Type type, Func<Type, IEnumerable<TInfo>> get)
        {
            var t = type;
            while (t != null)
            {
                var methInfos = get.Invoke(type);

                foreach (var method in methInfos)
                {
                    yield return method;
                }

                t = t.BaseType;
            }

            if (type is not { IsInterface: true }) yield break;
            {
                foreach (var i in type.GetInterfaces())
                {
                    foreach (var method in GetAllInfos(i, get))
                    {
                        yield return method;
                    }
                }
            }
        }

        public static IEnumerable<EventInfo> GetAllEvents(Type type)
        {
            return GetAllInfos(type, t => t.GetEvents(MethodFlags)).Distinct();
        }

        public static IEnumerable<MethodInfo> GetAllMethods(Type type)
        {
            return GetAllInfos(type, t => t.GetMethods(MethodFlags)).Distinct();
        }

        public static IEnumerable<(Type, IEnumerable<MethodInfo>)> GetAllMethodsAndInterfaces(Type type)
        {
            return type.GetInterfaces().Concat(new[] { type }).Select(i => (i, i.GetMethods())).Select(dummy => ((Type, IEnumerable<MethodInfo>))dummy);
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            return GetAllInfos(type, t => t.GetProperties(PropertyFlags)).Distinct();
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type type)
        {
            return GetAllInfos(type, t => t.GetFields(FieldFlags)).Distinct();
        }

        public static class FormatName
        {
            public static string FormatMethodName(MethodInfo p)
            {
                return
                    $"{p.Name}({string.Join(',', p.GetParameters().Select(pi => pi.ParameterType.Name))}): {p.ReturnType.Name}";
            }

            public static string FormatPropertyName(PropertyInfo p)
            {
                return $"{p.Name}: {p.PropertyType.Name}";
            }

            public static string FormatFieldName(FieldInfo p)
            {
                return $"{p.Name}: {p.FieldType.Name}";
            }

            public static string ExtractPropertyName(string formattedName)
            {
                return formattedName.Split(':').FirstOrDefault();
            }

            public static string ExtractFieldName(string formattedName)
            {
                return formattedName.Split(':').FirstOrDefault();
            }

            public static string ExtractMethodName(string formattedName)
            {
                return formattedName.Split('(').FirstOrDefault();
            }

            public static string Extract(string formattedName)
            {
                return formattedName.Contains('(')
                    ? formattedName.Split('(').FirstOrDefault()
                    : formattedName.Split(':').FirstOrDefault();
            }
        }
    }
}
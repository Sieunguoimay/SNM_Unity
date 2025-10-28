using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sieunguoimay.Tools
{
    public partial class RuntimeObjectExpose
    {
        private const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance;
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
            var targetObject = _objectProvider.TargetObject;

            if (targetObject == null) return Array.Empty<ObjectExposedItem>();
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
                    value = fieldInfo.GetValue(targetObject);
                }
                catch (Exception)
                {
                    //ignore
                }

                exposedItems.Add(new ObjectExposedItem
                {
                    MemberName = FormatMemberName(fieldInfo),
                    DisplayMemberName = fieldInfo.Name,
                    DisplayValue = ValueToString(value),
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
                    value = propInfo.GetValue(targetObject);
                }
                catch (Exception)
                {
                    //ignore
                }

                exposedItems.Add(new ObjectExposedItem
                {
                    MemberName = FormatMemberName(propInfo),
                    DisplayMemberName = propInfo.Name,
                    DisplayValue = ValueToString(value),
                    IsPrimitive = IsPrimitive(propInfo.PropertyType),
                    Value = value,
                    MemberInfo = propInfo
                });
            }

            foreach (var methodInfo in _allMethods)
            {
                var methodName = TrySplitLastDot(methodInfo.Name, out _, out var last) ? last : methodInfo.Name;
                if (methodName.StartsWith("get_")) continue;

                if (methodInfo.GetParameters().Length == 0)
                {
                    exposedItems.Add(new ObjectExposedItem
                    {
                        MemberName = FormatMemberName(methodInfo),
                        DisplayMemberName = methodInfo.Name,
                        DisplayValue = methodInfo.ReturnType.Name,
                        IsPrimitive = false,
                        Value = methodInfo.ReturnType.Name,
                        MemberInfo = methodInfo
                    });
                }
            }

            var type = targetObject.GetType();
            if (IndexableTypeHelper.TryGetElementType(type, out _))
            {
                for (var i = 0; i < IndexableTypeHelper.GetElementCount(targetObject); i++)
                {
                    var value = IndexableTypeHelper.GetElementAtIndex(targetObject, i);
                    exposedItems.Add(new ObjectExposedItem
                    {
                        MemberName = $"[{i}]",
                        DisplayMemberName = $"[{i}]",
                        DisplayValue = ValueToString(value),
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
            var type = _objectProvider.TargetObject?.GetType();
            if (type == null)
            {
                _allFields = Array.Empty<FieldInfo>();
                _allProperties = Array.Empty<PropertyInfo>();
                _allMethods = Array.Empty<MethodInfo>();
            }
            else
            {
                _allFields = type.GetFields(BindingAttr);
                _allProperties = type.GetProperties(BindingAttr);
                _allMethods = type.GetMethods(BindingAttr);
            }
        }

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private static string FormatMemberName(MemberInfo info)
        {
            var fullName = info.ReflectedType.FullName;
            var memberName = info.Name;
            if (TrySplitLastDot(info.Name, out var before, out var after))
            {
                fullName = before;
                memberName = after;
            }
            return $"{BaseAndInterfacesHashResolver.GetShortHash(fullName)}.{memberName}";
        }

        public static MemberInfo GetMemberInfo(Type type, string memberName)
        {
            var result = TrySplitLastDot(memberName, out var before, out var after) ? after : "";

            var hashedType = BaseAndInterfacesHashResolver.FindByFullNameHash(type, before);

            var reflectionType = type;

            if (hashedType != null && hashedType.IsAssignableFrom(type))
            {
                reflectionType = hashedType;
            }

            return reflectionType?.GetMember(result, BindingAttr)?.FirstOrDefault();
        }

        private static bool TrySplitLastDot(string input, out string before, out string after)
        {
            before = "";
            after = "";

            if (string.IsNullOrEmpty(input))
                return false;

            var lastDot = input.LastIndexOf('.');
            if (lastDot < 0)
                return false;

            before = input[..lastDot];
            after = input[(lastDot + 1)..];
            return true;
        }

        public static string ValueToString(object value)
        {
            return value?.ToString() ?? GetDefaultValueString(value);
        }

        public static string GetDefaultValueString<T>(T _)
        {
            var defaultValue = default(T);
            return defaultValue == null ? "null" : defaultValue.ToString();
        }

        public static class BaseAndInterfacesHashResolver
        {
            public static Type FindByFullNameHash(
                Type root,
                string targetHash,
                int hashBytes = 4,
                StringComparison comparison = StringComparison.OrdinalIgnoreCase,
                bool includeSelf = false)
            {
                if (root == null) throw new ArgumentNullException(nameof(root));
                if (string.IsNullOrWhiteSpace(targetHash)) throw new ArgumentException("Hash required.", nameof(targetHash));

                var candidates = EnumerateBaseAndDirectInterfaces(root, includeSelf);

                foreach (var t in candidates)
                {
                    var name = t.FullName ?? t.ToString(); // consistent with how you hashed
                    if (name is null) continue;

                    var h = GetShortHash(name, hashBytes);
                    if (string.Equals(h, targetHash, comparison))
                        return t;
                }
                return null;
            }

            public static IEnumerable<Type> EnumerateBaseAndDirectInterfaces(Type type, bool includeSelf = false)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                var baseTypes = new List<Type>();
                var current = includeSelf ? type : type.BaseType;
                while (current != null)
                {
                    baseTypes.Add(current);
                    current = current.BaseType;
                }

                var interfaces = type.GetInterfaces();

                return baseTypes.Concat(interfaces).Distinct();
            }

            public static string GetShortHash(string text, int hashBytes = 4)
            {
                using var sha1 = SHA1.Create();
                var data = Encoding.UTF8.GetBytes(text);
                var hash = sha1.ComputeHash(data);
                if (hashBytes < 1 || hashBytes > hash.Length) hashBytes = Math.Clamp(hashBytes, 1, hash.Length);

                var sb = new StringBuilder(hashBytes * 2);
                for (int i = 0; i < hashBytes; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

    }
}
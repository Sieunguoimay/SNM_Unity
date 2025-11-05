using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Snm.Tools.ObjectBrowser
{
    public enum ReflectionFilterType
    {
        IncludeBaseTypes,
        DeclaringTypeOnly,
    }

    public enum MemberFilterType
    {
        AllMembers,
        Method,
        Property,
        Field,
    }

    public class ReflectionExtractor
    {
        public const BindingFlags BindingAttr = BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        private readonly ReflectionFilterType filterType;
        private readonly MemberFilterType memberFilterType;

        public ReflectionExtractor(
            ReflectionFilterType filterType,
            MemberFilterType memberFilterType)
        {
            this.filterType = filterType;
            this.memberFilterType = memberFilterType;
        }

        public IEnumerable<MemberInfo> Extract(Type type)
        {
            var includeBaseTypes = filterType == ReflectionFilterType.IncludeBaseTypes;

            while (type != null)
            {
                foreach (var m in type.GetMembers(BindingAttr))
                {
                    if (Filter(m, memberFilterType))
                    {
                        yield return m;
                    }
                }

                if (!includeBaseTypes)
                    break;

                foreach (var iType in type.GetInterfaces())
                {
                    foreach (var m in iType.GetMembers(BindingAttr))
                    {
                        if (Filter(m, memberFilterType))
                        {
                            yield return m;
                        }
                    }
                }

                type = type.BaseType;
            }
        }

        public static bool Filter(MemberInfo memberInfo, MemberFilterType memberFilterType)
        {
            return memberFilterType switch
            {
                MemberFilterType.AllMembers => true,
                MemberFilterType.Field => memberInfo is FieldInfo,
                MemberFilterType.Property => memberInfo is PropertyInfo,
                MemberFilterType.Method => memberInfo is MethodInfo,
                _ => false,
            };
        }
    }

    public partial class ObjectReflectionExposer
    {
        public static IEnumerable<ObjectExposedItem> ExposeObject(
            object targetObject,
            Type reflectionType,
            ReflectionExtractor typeExtractor)
        {
            if (reflectionType == null) yield break;

            var allMembers = typeExtractor.Extract(reflectionType)
                .OrderBy(m => m is FieldInfo ? 0 : (m is PropertyInfo ? 1 : 2));

            foreach (var memberInfo in allMembers)
            {
                if (memberInfo is FieldInfo fieldInfo)
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

                    yield return new ObjectExposedItem
                    {
                        MemberName = FormatMemberName(fieldInfo),
                        DisplayMemberName = FormatDisplayMemberName(fieldInfo),
                        DisplayValue = ValueToString(value),
                        IsPrimitive = IsPrimitive(fieldInfo.FieldType),
                        Value = value,
                        MemberInfo = fieldInfo
                    };
                }

                if (memberInfo is PropertyInfo propInfo)
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

                    yield return new ObjectExposedItem
                    {
                        MemberName = FormatMemberName(propInfo),
                        DisplayMemberName = FormatDisplayMemberName(propInfo),
                        DisplayValue = ValueToString(value),
                        IsPrimitive = IsPrimitive(propInfo.PropertyType),
                        Value = value,
                        MemberInfo = propInfo
                    };
                }

                if (memberInfo is MethodInfo methodInfo)
                {
                    if (methodInfo.IsSpecialName) continue;
                    if (methodInfo.GetParameters().Length == 0)
                    {
                        yield return new ObjectExposedItem
                        {
                            MemberName = FormatMemberName(methodInfo),
                            DisplayMemberName = FormatDisplayMemberName(methodInfo),
                            DisplayValue = methodInfo.ReturnType.Name,
                            IsPrimitive = false,
                            Value = methodInfo.ReturnType.Name,
                            MemberInfo = methodInfo
                        };
                    }
                }
            }

            if (IndexableTypeHelper.TryGetElementType(reflectionType, out _))
            {
                for (var i = 0; i < IndexableTypeHelper.GetElementCount(targetObject); i++)
                {
                    var value = IndexableTypeHelper.GetElementAtIndex(targetObject, i);
                    yield return new ObjectExposedItem
                    {
                        MemberName = $"[{i}]",
                        DisplayMemberName = $"[{i}]",
                        DisplayValue = ValueToString(value),
                        IsPrimitive = IsPrimitive(value.GetType()),
                        Value = value,
                        MemberInfo = null
                    };
                }
            }
        }

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private static string FormatDisplayMemberName(MemberInfo info)
        {
            var shouldDisplayReflectionType = false;
            var isStatic = false;
            var isConst = false;

            if (info.DeclaringType != info.ReflectedType)
            {
                if (info is FieldInfo fi)
                {
                    shouldDisplayReflectionType = fi.IsPrivate;
                }
                else if (info is PropertyInfo pi)
                {
                    var acc = pi.GetAccessors(nonPublic: true);
                    shouldDisplayReflectionType = acc.Length > 0 && acc.All(m => m.IsPrivate);
                }
                else if (info is MethodInfo mi)
                {
                    shouldDisplayReflectionType = mi.IsPrivate;
                }
            }

            {
                if (info is FieldInfo fi)
                {
                    isStatic = fi.IsStatic;
                    isConst = fi.IsLiteral && !fi.IsInitOnly;
                }
                else if (info is MethodInfo mi)
                {
                    isStatic = mi.IsStatic;
                }
            }

            var declaringTypeTag = shouldDisplayReflectionType
                ? $" [of: {info.DeclaringType.Name}]" : "";
            var staticTag = isStatic ? " [static]" : "";
            var constTag = isConst ? " [const]" : "";
            return info.Name + declaringTypeTag + staticTag + constTag;
        }

        private static string FormatMemberName(MemberInfo info)
        {
            var reflectedType = info.ReflectedType.FullName;
            var reflectedTypeHash = BaseAndInterfacesHashResolver.GetShortHash(reflectedType);
            var memberName = info.Name;
            if (TrySplitLastDot(memberName, out var before, out var after))
            {
                memberName = after;
                var interfaceHash = BaseAndInterfacesHashResolver.GetShortHash(before);
                return $"{reflectedTypeHash}.{interfaceHash}.{memberName}";
            }

            return $"{reflectedTypeHash}.{memberName}";
        }

        public static MemberInfo GetMemberInfo(Type type, string memberName)
        {
            var parts = memberName.Split('.');
            var partCount = parts.Length;
            if (partCount == 2)
            {
                var reflectedTypeHash = parts[0];
                var mName = parts[1];
                var reflectedType = BaseAndInterfacesHashResolver.FindByFullNameHash(type, reflectedTypeHash);
                return reflectedType?.GetMember(mName, ReflectionExtractor.BindingAttr)?.FirstOrDefault();
            }
            else if (partCount == 3)
            {
                var reflectedTypeHash = parts[0];
                var interfaceHash = parts[1];
                var mName = parts[2];
                var reflectedType = BaseAndInterfacesHashResolver.FindByFullNameHash(type, reflectedTypeHash);
                var interfaceType = BaseAndInterfacesHashResolver.FindByFullNameHash(type, interfaceHash);
                var mFullName = interfaceType.FullName + "." + mName;
                return reflectedType?.GetMember(mFullName, ReflectionExtractor.BindingAttr)?.FirstOrDefault();
            }
            else
            {
                return null;
            }
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
    }

    public static class BaseAndInterfacesHashResolver
    {
        public static Type FindByFullNameHash(
            Type root,
            string targetHash,
            int hashBytes = 4,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase,
            bool includeSelf = true)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrWhiteSpace(targetHash))
                throw new ArgumentException("Hash required.", nameof(targetHash));

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
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
        DeclaredOnly,
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

        public ReflectionExtractor(ReflectionFilterType filterType, MemberFilterType memberFilterType)
        {
            this.filterType = filterType;
            this.memberFilterType = memberFilterType;
        }

        public IEnumerable<MemberInfo> Extract(Type type)
        {
            return GetAllMemberInfos(type, filterType == ReflectionFilterType.IncludeBaseTypes)
                .Where(m => Filter(m, memberFilterType));
        }

        public static IEnumerable<MemberInfo> GetAllMemberInfos(Type type, bool includeBaseTypes)
        {
            while (type != null)
            {
                foreach (var m in type.GetMembers(BindingAttr))
                {
                    yield return m;
                }

                if (!includeBaseTypes)
                    break;

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
        private const BindingFlags BindingAttr = BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.NonPublic;

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

        private static string FormatDisplayMemberName(MemberInfo info)
        {
            var shouldDisplayReflectionType = false;
            var isStatic = false;
            if (info.DeclaringType != info.ReflectedType)
            {
                if (info is FieldInfo fi)
                {
                    shouldDisplayReflectionType = fi.IsPrivate;
                    isStatic = fi.IsStatic;
                }
                else if (info is PropertyInfo pi)
                {
                    var acc = pi.GetAccessors(nonPublic: true);
                    shouldDisplayReflectionType = acc.Length > 0 && acc.All(m => m.IsPrivate);
                }
                else if (info is MethodInfo mi)
                {
                    shouldDisplayReflectionType = mi.IsPrivate;
                    isStatic = mi.IsStatic;
                }
            }

            var declaringTypeTag = shouldDisplayReflectionType
                ? $" [of: {info.DeclaringType.Name}]" : "";
            var staticTag = isStatic ? " [static]" : "";
            return info.Name + declaringTypeTag + staticTag;
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
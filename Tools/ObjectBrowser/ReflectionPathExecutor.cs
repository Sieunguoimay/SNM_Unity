using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Snm.Tools.ObjectBrowser
{
    public partial class ReflectionPathExecutor
    {
        private readonly MemberInfoWrapper[] executablePath;
        private readonly object sourceObject;

        public ReflectionPathExecutor(string path, object sourceObject, Type reflectionType)
        {
            this.sourceObject = sourceObject;
            executablePath = CreateExecutablePath(sourceObject, reflectionType, path);
        }

        public object ExecutePath()
        {
            return ExecutePath(executablePath, sourceObject);
        }

        public Type GetFinalReflectionType()
        {
            return executablePath.Length > 0
                ? executablePath[^1].GetMemberType()
                : null;
        }

        private static MemberInfoWrapper[] CreateExecutablePath(object sourceObject, Type reflectionType, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<MemberInfoWrapper>();
            }

            var pathSegments = path.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var memberInfos = new MemberInfoWrapper[pathSegments.Length];

            var currObj = reflectionType != null ? null : sourceObject;
            var currType = reflectionType != null ? reflectionType : sourceObject.GetType();

            for (var i = 0; i < pathSegments.Length; i++)
            {
                memberInfos[i] = new MemberInfoWrapper(currType, pathSegments[i]);
                currObj = memberInfos[i].GetMemberValue(currObj);
                currType = currObj?.GetType() ?? memberInfos[i].GetMemberType();
            }
            return memberInfos;
        }

        private static object ExecutePath(IEnumerable<MemberInfoWrapper> path, object rootObject)
        {
            return path.Aggregate(rootObject, (current, mi) => mi.GetMemberValue(current));
        }

        private class MemberInfoWrapper
        {
            private readonly Type memberType;
            private readonly Func<object, object> getValueFunc;

            public MemberInfoWrapper(Type type, string memberName)
            {
                if (type == null) return;

                if (IndexableTypeHelper.TryGetElementType(type, out var elementType))
                {
                    if (int.TryParse(memberName.Trim('[', ']'), out var i))
                    {
                        memberType = elementType;
                        getValueFunc = obj => IndexableTypeHelper.GetElementAtIndex(obj, i);
                        return;
                    }
                }

                var memberInfo = ObjectReflectionExposer.GetMemberInfo(type, memberName);

                if (memberInfo is FieldInfo fi)
                {
                    memberType = fi.FieldType;
                    getValueFunc = obj => fi.GetValue(obj);
                }
                else if (memberInfo is PropertyInfo pi)
                {
                    memberType = pi.PropertyType;
                    getValueFunc = obj => pi.GetValue(obj);
                }
                else if (memberInfo is MethodInfo mi)
                {
                    memberType = mi.ReturnType;
                    getValueFunc = obj => mi.Invoke(obj, null);
                }
            }

            public Type GetMemberType() => memberType;

            public object GetMemberValue(object obj)
            {
                try
                {
                    return getValueFunc?.Invoke(obj);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
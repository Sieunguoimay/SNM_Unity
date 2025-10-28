using System;
using System.Linq;
using System.Reflection;

namespace Sieunguoimay.Tools
{
    public partial class ReflectionPathExecutor
    {
        private MemberInfoWrapper[] _executablePath;
        private object _sourceObject;

        public void Setup(string path, object sourceObject)
        {
            _sourceObject = sourceObject;
            _executablePath = CreateExecutablePath(sourceObject, path);
        }

        private static MemberInfoWrapper[] CreateExecutablePath(object sourceObject, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Array.Empty<MemberInfoWrapper>();
            }

            var pathSegments = path.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var memberInfos = new MemberInfoWrapper[pathSegments.Length];

            var currObj = sourceObject;
            var currType = sourceObject.GetType();

            for (var i = 0; i < pathSegments.Length; i++)
            {
                memberInfos[i] = new MemberInfoWrapper(currType, pathSegments[i]);
                currObj = memberInfos[i].GetMemberValue(currObj);
                currType = currObj?.GetType() ?? memberInfos[i].GetMemberType();
            }
            return memberInfos;
        }

        public object ExecutePath()
        {
            return _executablePath.Aggregate(_sourceObject, (current, mi) => mi.GetMemberValue(current));
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

                var memberInfo = RuntimeObjectExpose.GetMemberInfo(type, memberName);

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
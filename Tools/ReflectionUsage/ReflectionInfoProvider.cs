#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ReflectionUsage
{

    public abstract class ReflectionInfoProvider
    {
        public abstract IEnumerable<ReflectionInfo> GetReflectionInfos(UnityEngine.Object obj);

        private static IEnumerable<ReflectionInfoProvider> _subProviders;
        public static IEnumerable<ReflectionInfoProvider> SubProviders => _subProviders ??= GetSubProviders().ToArray();

        static IEnumerable<ReflectionInfoProvider> GetSubProviders()
        {
            foreach (var s in GetSubclasses(typeof(ReflectionInfoProvider)))
            {
                yield return Activator.CreateInstance(s) as ReflectionInfoProvider;
            }
        }

        static IEnumerable<Type> GetSubclasses(Type type)
        {
            var subclasses = Assembly.GetAssembly(type)
                .GetTypes()
                .Where(t => t.IsSubclassOf(type));

            return subclasses;
        }
    }
}
#endif
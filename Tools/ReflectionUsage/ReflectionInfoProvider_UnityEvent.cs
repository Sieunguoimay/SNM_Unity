#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace ReflectionUsage
{
    public class ReflectionInfoProvider_UnityEvent : ReflectionInfoProvider
    {
        public override IEnumerable<ReflectionInfo> GetReflectionInfos(UnityEngine.Object obj)
            => GetReflectionInfos_UnityEvent(obj);

        private static IEnumerable<ReflectionInfo> GetReflectionInfos_UnityEvent(UnityEngine.Object o)
        {
            return GetFieldInfos(o.GetType(), typeof(UnityEngine.Events.UnityEvent))
                .Select(field =>
                {
                    var v = field.GetValue(o);
                    if (v is UnityEngine.Events.UnityEvent ue)
                    {
                        for (var i = 0; i < ue.GetPersistentEventCount();)
                        {
                            var target = ue.GetPersistentTarget(i);
                            var method = ue.GetPersistentMethodName(i);
                            return new ReflectionInfo
                            {
                                Type = target.GetType(),
                                member = method + $" (event: {field.Name})"
                            };
                        }
                    }
                    return null;
                }).Where(r => r != null);
        }
        private static IEnumerable<FieldInfo> GetFieldInfos(Type t, Type fieldType)
        {
            var fields = t.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.FieldType.Equals(fieldType) || f.FieldType.IsSubclassOf(fieldType))
                {
                    yield return f;
                }
            }
        }
    }
}
#endif
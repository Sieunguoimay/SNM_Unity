using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DependencyInjection
{
    public static class DependencyInjector
    {
        private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static void Inject(object target, Dictionary<string, object> keyValuePairs)
        {
            var type = target.GetType();
            foreach (var key in keyValuePairs.Keys)
            {
                var field = GetFieldInfo(type, key);
                if (field == null)
                {
                    Debug.LogError($"Failed to inject! Field not exists {key}");
                }
                else
                {
                    if (field.GetCustomAttribute<InjectFieldAttribute>() == null)
                    {
                        Debug.LogError($"Trying to inject value into non InjectedField");
                    }

                    field.SetValue(target, keyValuePairs[key]);
                }
            }

            if (target is IInjectionListener listener)
            {
                listener.OnInjected();
            }
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            var current = type;
            while (current != null)
            {
                var fieldInfo = current.GetField(fieldName, Flags);
                if (fieldInfo != null)
                {
                    return fieldInfo;
                }
                current = current.BaseType;
            }
            return null;
        }
    }
}
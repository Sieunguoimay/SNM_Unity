using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{
    public class SimpleDependencyResolver : IDepedencyResolver
    {
        private readonly Dictionary<Type, object> instances;

        public SimpleDependencyResolver()
        {
            instances = new();
        }

        public SimpleDependencyResolver(Dictionary<Type, object> instances)
        {
            this.instances = instances;
        }

        public void AddInstance<T>(T instance)
        {
            var type = typeof(T);
            if (instances.ContainsKey(type))
            {
                Debug.LogError($"Failed to AddInstance. Type {type.Name} is already exist in the list.");
            }
            else
            {
                instances.Add(typeof(T), instance);
            }
        }

        public T Resolve<T>()
        {
            if (instances.TryGetValue(typeof(T), out var instance))
            {
                return (T)instance;
            }

            Debug.LogError($"Failed to resolve for type {typeof(T).Name}");
            return default;
        }
    }
}

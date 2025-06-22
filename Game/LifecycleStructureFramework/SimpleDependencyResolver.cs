using System;
using System.Collections.Generic;

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
            instances.Add(typeof(T), instance);
        }

        public T Resolve<T>()
        {
            return (T)instances[typeof(T)];
        }
    }
}

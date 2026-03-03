using System;
using System.Collections.Generic;
using System.Linq;

namespace Snm.App.DependencyInjection
{
    public sealed class RuntimeContainer : IResolver, IDisposable
    {
        private readonly Dictionary<(Type, string), List<Binding>> bindings;
        private readonly RuntimeContainer parent;

        private readonly Dictionary<Binding, object> singletonInstances = new();
        private readonly Dictionary<Binding, object> scopedInstances = new();
        private readonly List<RuntimeContainer> children = new();
        private readonly HashSet<Binding> resolutionStack = new();

        private bool IsRoot => parent == null;

        internal RuntimeContainer(
            Dictionary<(Type, string), List<Binding>> bindings,
            RuntimeContainer parent = null)
        {
            this.bindings = bindings;
            this.parent = parent;
        }

        public T Resolve<T>(string id = null)
            where T : class
        {
            var binding = GetBinding(typeof(T), id);

            var instance = (T)ResolveBinding(binding);

            return instance;
        }

        public T[] ResolveAll<T>() where T : class
        {
            var type = typeof(T);
            var allBindings = GetAllBindings(type);

            return allBindings
                .Select(b => (T)ResolveBinding(b))
                .ToArray();
        }

        public void Dispose()
        {
            // Dispose children first
            for (int i = children.Count - 1; i >= 0; i--)
                children[i].Dispose();

            children.Clear();

            // Dispose scoped instances
            foreach (var instance in scopedInstances.Values.OfType<IDisposable>())
                instance.Dispose();

            scopedInstances.Clear();

            // Only entryPoint disposes singletons
            if (IsRoot)
            {
                foreach (var instance in singletonInstances.Values.OfType<IDisposable>())
                    instance.Dispose();

                singletonInstances.Clear();
            }
        }

        public RuntimeContainer CreateScope()
        {
            var scope = new RuntimeContainer(bindings, this);
            children.Add(scope);
            return scope;
        }

        private Binding GetBinding(Type type, string id)
        {
            var key = (type, id);

            if (bindings.TryGetValue(key, out var list) && list.Count > 0)
                return list[0];

            if (parent != null)
                return parent.GetBinding(type, id);

            throw new InvalidOperationException(
                $"No binding found for {type.Name}");
        }

        private object ResolveBinding(Binding binding)
        {
            if (!resolutionStack.Add(binding))
                throw new InvalidOperationException(
                    $"Circular dependency detected for {binding.Type.Name}");
            try
            {
                return binding.Lifetime switch
                {
                    BindingLifetime.Transient => binding.CreateInstance(this),
                    BindingLifetime.Singleton => ResolveSingleton(binding),
                    BindingLifetime.Scoped => ResolveScoped(binding),
                    _ => throw new NotSupportedException(),
                };
            }
            finally
            {
                resolutionStack.Remove(binding);
            }
        }

        private object ResolveSingleton(Binding binding)
        {
            if (!IsRoot)
                return parent.ResolveSingleton(binding);

            if (singletonInstances.TryGetValue(binding, out var existing))
                return existing;

            var created = binding.CreateInstance(this);
            singletonInstances[binding] = created;

            return created;
        }

        private object ResolveScoped(Binding binding)
        {
            if (scopedInstances.TryGetValue(binding, out var existing))
                return existing;

            var created = binding.CreateInstance(this);
            scopedInstances[binding] = created;

            return created;
        }

        private IEnumerable<Binding> GetAllBindings(Type type)
        {
            var local = bindings
                .Where(kv => kv.Key.Item1 == type)
                .SelectMany(kv => kv.Value);

            if (parent == null)
                return local;

            return local.Concat(parent.GetAllBindings(type));
        }
    }
}
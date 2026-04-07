using System;
using System.Collections.Generic;
using System.Linq;

namespace Snm.DependencyInjection
{
    public sealed class RuntimeContainer : IResolver, IScope
    {
        private readonly Dictionary<(Type, string), List<Binding>> bindings;
        private readonly RuntimeContainer parent;

        private readonly Dictionary<Binding, object> singletonInstances = new();
        private readonly List<object> singletonCreationOrder = new();
        private readonly Dictionary<Binding, object> scopedInstances = new();
        private readonly List<object> scopedCreationOrder = new();
        private readonly List<RuntimeContainer> children = new();
        private readonly HashSet<Binding> resolutionStack = new();

        private bool IsRoot => parent == null;

        public IResolver Resolver => this;

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

        public T[] ResolveAllLocal<T>() where T : class
        {
            var type = typeof(T);
            var localBindings = GetLocalBindings(type);

            return localBindings
                .Select(b => (T)ResolveBinding(b))
                .ToArray();
        }

        public void Dispose()
        {
            // Dispose children first
            for (int i = children.Count - 1; i >= 0; i--)
                children[i].Dispose();

            children.Clear();

            // Dispose scoped instances in reverse creation order
            for (int i = scopedCreationOrder.Count - 1; i >= 0; i--)
            {
                if (scopedCreationOrder[i] is IDisposable disposable && disposable != this)
                    disposable.Dispose();
            }

            scopedCreationOrder.Clear();
            scopedInstances.Clear();

            // Dispose singletons owned by this container
            for (int i = singletonCreationOrder.Count - 1; i >= 0; i--)
            {
                if (singletonCreationOrder[i] is IDisposable disposable)
                    disposable.Dispose();
            }

            singletonCreationOrder.Clear();
            singletonInstances.Clear();
        }

        public IScope CreateChildScope()
        {
            var scope = new RuntimeContainer(bindings, this);
            children.Add(scope);
            return scope;
        }

        public IScope CreateChildScope(Action<IBindingContext> configure)
        {
            var builder = new ContainerBuilder();
            configure(builder);

            RuntimeContainer scope = null;
            builder.Bind<IScope>().ToFactory(_ => scope).AsScoped();

            scope = builder.Build(this);

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

        private bool OwnsBinding(Binding binding)
        {
            var key = (binding.Type, binding.Id);
            return bindings.TryGetValue(key, out var list) && list.Contains(binding);
        }

        private object ResolveSingleton(Binding binding)
        {
            // Bubble up ONLY if this container doesn't own the binding
            if (!IsRoot && !OwnsBinding(binding))
                return parent.ResolveSingleton(binding);

            if (singletonInstances.TryGetValue(binding, out var existing))
                return existing;

            var created = binding.CreateInstance(this);
            singletonInstances[binding] = created;
            singletonCreationOrder.Add(created);

            return created;
        }

        private object ResolveScoped(Binding binding)
        {
            if (scopedInstances.TryGetValue(binding, out var existing))
                return existing;

            var created = binding.CreateInstance(this);
            scopedInstances[binding] = created;
            scopedCreationOrder.Add(created);

            return created;
        }

        private IEnumerable<Binding> GetLocalBindings(Type type)
        {
            return bindings
                .Where(kv => kv.Key.Item1 == type)
                .SelectMany(kv => kv.Value);
        }

        private IEnumerable<Binding> GetAllBindings(Type type)
        {
            var local = GetLocalBindings(type);

            if (parent == null)
                return local;

            return local.Concat(parent.GetAllBindings(type));
        }
    }
}
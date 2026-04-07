using System;
using System.Collections.Generic;

namespace Snm.DependencyInjection
{
    public sealed class ContainerBuilder : IBindingContext
    {
        private readonly Dictionary<(Type,string), List<Binding>> _bindings
            = new();

        private readonly List<IBindingBuilder> _builders = new();

        private bool _built;

        public BindingBuilder<T> Bind<T>(string id = null)
            where T : class
        {
            EnsureNotBuilt();
            var builder = new BindingBuilder<T>(this, id);
            _builders.Add(builder);
            return builder;
        }

        internal void AddBinding(Binding binding)
        {
            var key = (binding.Type, binding.Id);

            if (!_bindings.TryGetValue(key, out var list))
            {
                list = new List<Binding>();
                _bindings[key] = list;
            }

            list.Add(binding);
        }

        public RuntimeContainer Build()
        {
            return BuildInternal(null);
        }

        internal RuntimeContainer Build(RuntimeContainer parent)
        {
            return BuildInternal(parent);
        }

        private RuntimeContainer BuildInternal(RuntimeContainer parent)
        {
            EnsureNotBuilt();

            _built = true;

            foreach (var builder in _builders)
            {
                if (!builder.Completed)
                    throw new InvalidOperationException(
                        $"Binding for {builder.BoundType.Name} was started but never completed. " +
                        "Did you forget to call ToInstance() or ToFactory().AsSingleton()/AsTransient()/AsScoped()?");
            }

            return new RuntimeContainer(
                new Dictionary<(Type,string), List<Binding>>(_bindings),
                parent);
        }

        private void EnsureNotBuilt()
        {
            if (_built)
                throw new InvalidOperationException(
                    "Container already built.");
        }
    }
}

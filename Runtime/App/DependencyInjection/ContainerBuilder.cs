using System;
using System.Collections.Generic;

namespace Snm.App.DependencyInjection
{
    public sealed class ContainerBuilder : IContainer, IBindingContext
    {
        private readonly Dictionary<(Type,string), List<Binding>> _bindings
            = new();

        private bool _built;

        public BindingBuilder<T> Bind<T>(string id = null)
            where T : class
        {
            EnsureNotBuilt();
            return new BindingBuilder<T>(this, id);
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
            EnsureNotBuilt();

            _built = true;

            // optional: validation pass here

            return new RuntimeContainer(
                new Dictionary<(Type,string), List<Binding>>(_bindings));
        }

        private void EnsureNotBuilt()
        {
            if (_built)
                throw new InvalidOperationException(
                    "Container already built.");
        }
    }
}
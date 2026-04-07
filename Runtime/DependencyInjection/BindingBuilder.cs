using System;
using System.Collections.Generic;

namespace Snm.DependencyInjection
{
    internal interface IBindingBuilder
    {
        bool Completed { get; }
        Type BoundType { get; }
    }

    public sealed class BindingBuilder<T> : IBindingBuilder where T : class
    {
        private readonly ContainerBuilder _container;
        private readonly string _id;
        private List<(Type type, string id)> _additionalTypes;

        public bool Completed { get; private set; }
        public Type BoundType => typeof(T);

        internal BindingBuilder(ContainerBuilder container, string id)
        {
            _container = container;
            _id = id;
        }

        public BindingBuilder<T> AlsoBind<U>(string id = null) where U : class
        {
            _additionalTypes ??= new List<(Type, string)>();
            _additionalTypes.Add((typeof(U), id));
            return this;
        }

        public void ToInstance(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Complete(_ => instance, BindingLifetime.Singleton);
        }

        public LifetimeBuilder<T> ToFactory(Func<IResolver, T> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return new LifetimeBuilder<T>(this, factory);
        }

        internal void Complete(
            Func<IResolver, object> factory,
            BindingLifetime lifetime)
        {
            if (Completed)
                throw new InvalidOperationException(
                    $"Binding for {typeof(T).Name} already configured.");

            Completed = true;

            var binding = new Binding(
                typeof(T),
                _id,
                factory,
                lifetime);

            _container.AddBinding(binding);

            if (_additionalTypes != null)
            {
                foreach (var (type, id) in _additionalTypes)
                {
                    var forwarding = new Binding(
                        type,
                        id,
                        r => r.Resolve<T>(_id),
                        lifetime);

                    _container.AddBinding(forwarding);
                }
            }
        }
    }
}

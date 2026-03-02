using System;

namespace Snm.App.DependencyInjection
{
    internal sealed class Binding
    {
        public enum Lifetime
        {
            Transient,
            Singleton
        }

        public Type Type { get; }
        public string Id { get; }

        private readonly Func<IResolver, object> factory;
        private readonly Lifetime lifetime;

        private object _singletonInstance;

        public Binding(
            Type type,
            string id,
            Func<IResolver, object> factory,
            Lifetime lifetime)
        {
            Type = type;
            Id = id;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.lifetime = lifetime;
        }

        public object Resolve(IResolver resolver)
        {
            if (lifetime == Lifetime.Singleton)
            {
                _singletonInstance ??= factory(resolver);
                return _singletonInstance;
            }

            return factory(resolver);
        }
    }
}
using System;

namespace Snm.Runtime.App.DependencyInjection
{

    public enum BindingLifetime
    {
        Transient,
        Singleton,
        Scoped
    }
    
    internal sealed class Binding
    {
        public Type Type { get; }
        public string Id { get; }

        private readonly Func<IResolver, object> factory;
        private readonly BindingLifetime lifetime;

        public BindingLifetime Lifetime => lifetime;

        public Binding(
            Type type,
            string id,
            Func<IResolver, object> factory,
            BindingLifetime lifetime)
        {
            Type = type;
            Id = id;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.lifetime = lifetime;
        }

        public object CreateInstance(IResolver resolver)
        {
            return factory(resolver);
        }
    }
}
using System;

namespace Snm.DependencyInjection
{
    public sealed class LifetimeBuilder<T> where T : class
    {
        private readonly BindingBuilder<T> _builder;
        private readonly Func<IResolver, T> _factory;

        internal LifetimeBuilder(BindingBuilder<T> builder, Func<IResolver, T> factory)
        {
            _builder = builder;
            _factory = factory;
        }

        public void AsSingleton()
        {
            _builder.Complete(r => _factory(r), BindingLifetime.Singleton);
        }

        public void AsTransient()
        {
            _builder.Complete(r => _factory(r), BindingLifetime.Transient);
        }

        public void AsScoped()
        {
            _builder.Complete(r => _factory(r), BindingLifetime.Scoped);
        }
    }
}

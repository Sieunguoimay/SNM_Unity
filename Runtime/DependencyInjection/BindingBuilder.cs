using System;

namespace Snm.DependencyInjection
{
    public sealed class BindingBuilder<T> where T : class
    {
        private readonly ContainerBuilder container;
        private readonly string id;
        private bool _completed;

        internal BindingBuilder(ContainerBuilder container, string id)
        {
            this.container = container;
            this.id = id;
        }

        public void ToInstance(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Complete(_ => instance, BindingLifetime.Singleton);
        }

        public void ToSingleton(Func<IResolver, T> factory)
        {
            Complete(r => factory(r), BindingLifetime.Singleton);
        }

        public void ToTransient(Func<IResolver, T> factory)
        {
            Complete(r => factory(r), BindingLifetime.Transient);
        }
        
        public void ToScoped(Func<IResolver, T> factory)
        {
            Complete(r => factory(r), BindingLifetime.Scoped);
        }

        private void Complete(
            Func<IResolver, object> factory,
            BindingLifetime lifetime)
        {
            if (_completed)
                throw new InvalidOperationException(
                    $"Binding for {typeof(T).Name} already configured.");

            _completed = true;

            var binding = new Binding(
                typeof(T),
                id,
                factory,
                lifetime);

            container.AddBinding(binding);
        }
    }
}
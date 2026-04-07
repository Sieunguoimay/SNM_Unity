using System;

namespace Snm.DependencyInjection
{
    public interface IScope : IDisposable
    {
        IResolver Resolver { get; }
        IScope CreateChildScope(Action<IBindingContext> configure);
    }
}

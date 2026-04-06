using System;

namespace Snm.DependencyInjection
{
    public interface IScopeFactory
    {
        IManagedScope CreateScope(Action<IBindingContext> configure);
    }
}

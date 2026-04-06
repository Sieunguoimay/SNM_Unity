using System;

namespace Snm.DependencyInjection
{
    public interface IManagedScope : IResolver, IDisposable
    {
    }
}

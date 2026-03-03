using System;
using System.Collections.Generic;

namespace Snm.App.Lifecycle
{
    public interface IDependentLifecycle
    {
        IReadOnlyList<Type> Dependencies { get; }
    }
}
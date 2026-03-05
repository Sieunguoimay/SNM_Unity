using System;
using System.Collections.Generic;

namespace Snm.Runtime.App.Lifecycle
{
    public interface IDependentLifecycle
    {
        IReadOnlyList<Type> Dependencies { get; }
    }
}
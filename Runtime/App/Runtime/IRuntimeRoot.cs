using System;
using System.Collections.Generic;

namespace Snm.App.Runtime
{
    public interface IRuntimeRoot
    {
        IReadOnlyList<Type> Dependencies { get; }
        int Order => 0; // optional fine-tuning inside same level

        void Start();
    }
}
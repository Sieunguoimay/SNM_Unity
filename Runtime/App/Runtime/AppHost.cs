using System;
using System.ComponentModel;
using Snm.App.DependencyInjection;

namespace Snm.App.Runtime
{
    public sealed class AppHost
    {
        private readonly IResolver resolver;

        public AppHost(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public void Start()
        {
            var runtimeRoots = resolver.ResolveAll<IRuntimeRoot>();

            foreach (var root in runtimeRoots)
            {
                root.Start();
            }
        }

        public void Stop()
        {
            (resolver as IDisposable)?.Dispose();
        }
    }
}
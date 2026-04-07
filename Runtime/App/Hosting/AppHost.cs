using System;
using Snm.DependencyInjection;
using Snm.Runtime.App.Lifecycle;

namespace Snm.Runtime.App.Hosting
{
    public sealed class AppHost
    {
        private readonly IScope scope;
        private readonly LifecycleService lifecycle;

        public AppHost(LifecycleService lifecycle, IScope scope)
        {
            this.lifecycle = lifecycle;
            this.scope = scope;
        }

        public void Start()
        {
            lifecycle.Initialize();
            lifecycle.Start();
        }

        public void Stop()
        {
            lifecycle.Stop();
            scope.Dispose();
        }
    }
}
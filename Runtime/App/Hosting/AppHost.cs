using System;
using Snm.DependencyInjection;
using Snm.Runtime.App.Lifecycle;

namespace Snm.Runtime.App.Hosting
{
    public sealed class AppHost
    {
        private readonly RuntimeContainer container;
        private readonly LifecycleService lifecycle;

        public AppHost(LifecycleService lifecycle, RuntimeContainer container)
        {
            this.lifecycle = lifecycle;
            this.container = container;
        }

        public void Start()
        {
            lifecycle.Initialize();
            lifecycle.Start();
        }

        public void Stop()
        {
            lifecycle.Stop();
            container.Dispose();
        }
    }
}
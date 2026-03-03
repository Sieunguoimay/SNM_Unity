using System;
using Snm.App.DependencyInjection;
using Snm.App.Lifecycle;

namespace Snm.App.Hosting
{
    public sealed class AppHost
    {
        private readonly RuntimeContainer container;
        private LifecycleService _lifecycle;

        public AppHost(RuntimeContainer container)
        {
            this.container = container;
        }

        public void Start()
        {
            _lifecycle = container.Resolve<LifecycleService>();
            _lifecycle.Initialize();
            _lifecycle.Start();
        }

        public void Stop()
        {
            _lifecycle.Stop();
            container.Dispose();
        }
    }
}
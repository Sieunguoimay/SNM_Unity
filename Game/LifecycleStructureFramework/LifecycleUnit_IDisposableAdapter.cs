using System;

namespace Snm.LifecycleStructureFramework
{
    public class LifecycleUnit_IDisposableAdapter : ILifecycleUnit
    {
        private readonly IDisposable disposable;

        public LifecycleUnit_IDisposableAdapter(IDisposable disposable)
        {
            this.disposable = disposable;
        }
        public void Initialize()
        {
        }

        public void Setup()
        {
        }

        public void Teardown()
        {
        }

        public void Cleanup()
        {
            disposable.Dispose();
        }
    }
}
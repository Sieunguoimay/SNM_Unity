using System;

namespace Snm.WaterSystem
{
    public class WaterSystemHandle : IDisposable
    {
        private readonly IDisposable dispose;

        public WaterSystemHandle(IDisposable dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            dispose.Dispose();
        }
    }
}
using System;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemHandle
    {
        private readonly Action destroyCallback;
        private readonly Action openDebugToolCallback;

        public GrassSystemHandle(
            Action destroyCallback,
            Action openDebugToolCallback)
        {
            this.destroyCallback = destroyCallback;
            this.openDebugToolCallback = openDebugToolCallback;
        }

        public void DestroySystem()
        {
            destroyCallback?.Invoke();
        }

        public void Editor_OpenDebugWindow() => openDebugToolCallback();
    }
}
using System;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemHandle
    {
        private readonly GrassTrampleBrushRegistry brushRegistry;
        private readonly Action destroyCallback;
        private readonly Action openDebugToolCallback;

        public GrassTrampleBrushRegistry BrushRegistry => brushRegistry;

        public GrassSystemHandle(
            GrassTrampleBrushRegistry brushRegistry,
            Action destroyCallback,
            Action openDebugToolCallback)
        {
            this.brushRegistry = brushRegistry;
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
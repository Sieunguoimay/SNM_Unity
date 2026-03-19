using System;
using System.Collections.Generic;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemHandle
    {
        private readonly GrassTrampleSystemHandle trampleHandle;
        private readonly Action destroyCallback;
        private readonly Action openDebugToolCallback;

        public GrassSystemHandle(
            GrassTrampleSystemHandle trampleHandle,
            Action destroyCallback,
            Action openDebugToolCallback)
        {
            this.trampleHandle = trampleHandle;
            this.destroyCallback = destroyCallback;
            this.openDebugToolCallback = openDebugToolCallback;
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            trampleHandle.SetDisturbers(disturbers);
        }

        public void DestroySystem()
        {
            destroyCallback?.Invoke();
        }

        public void Editor_OpenDebugWindow() => openDebugToolCallback();
    }
}

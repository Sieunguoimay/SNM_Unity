#if UNITY_EDITOR
using System;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionSystem : IDisposable
    {
        private readonly Action destroyCallback;

        public InspectorExtensionSystem(Action destroyCallback)
        {
            this.destroyCallback = destroyCallback;
        }

        public void Dispose() { destroyCallback?.Invoke(); }
    }
}
#endif
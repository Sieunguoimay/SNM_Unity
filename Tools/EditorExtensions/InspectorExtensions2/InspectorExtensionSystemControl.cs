#if UNITY_EDITOR
using System;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionSystemControl
    {
        public InspectorExtensionSystemDestroyer Destroyer { get; }

        public InspectorExtensionSystemControl(
            InspectorExtensionSystemDestroyer destroyer)
        {
            Destroyer = destroyer;
        }
    }

    public class InspectorExtensionSystemDestroyer
    {
        private readonly Action destroyCallback;

        public InspectorExtensionSystemDestroyer(Action destroyCallback)
        {
            this.destroyCallback = destroyCallback;
        }

        public void Destroy() { destroyCallback?.Invoke(); }
    }
}
#endif
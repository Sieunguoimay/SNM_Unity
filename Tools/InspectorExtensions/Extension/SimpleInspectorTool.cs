#if UNITY_EDITOR
using System;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SimpleInspectorTool : IInspectorTool, IDisposable
    {
        private readonly InspectorExtensionLocation location;
        private readonly Func<InspectorToolContext, VisualElement> buildVEFunc;
        private readonly Action disposeAction;

        public InspectorExtensionLocation Location => location;

        public SimpleInspectorTool(
            InspectorExtensionLocation location, 
            Func<InspectorToolContext, VisualElement> buildVEFunc,
            Action disposeAction)
        {
            this.location = location;
            this.buildVEFunc = buildVEFunc;
            this.disposeAction = disposeAction;
        }

        public void Dispose()
        {
            disposeAction?.Invoke();
        }

        public VisualElement BuildVE(InspectorToolContext context)
        {
            return buildVEFunc?.Invoke(context);
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SimpleInspectorTool : IInspectorTool
    {
        private readonly InspectorExtensionLocation location;
        private readonly Func<InspectorToolContext, VisualElement> buildVEFunc;

        public SimpleInspectorTool(InspectorExtensionLocation location, Func<InspectorToolContext, VisualElement> buildVEFunc)
        {
            this.location = location;
            this.buildVEFunc = buildVEFunc;
        }

        public InspectorExtensionLocation Location => location;

        public VisualElement BuildVE(InspectorToolContext context)
        {
            return buildVEFunc?.Invoke(context);
        }
    }
}
#endif
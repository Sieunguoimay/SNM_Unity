#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SimpleInspectorExtension : IInspectorExtension, IInspectorExtensionVEBuilder
    {
        private readonly Func<InspectorExtensionContext, VisualElement> buildVEFunc;
        private readonly IInspectorExtensionVEBuilder veBuilder;

        public InspectorExtensionLocation Location { get; }
        public IEnumerable<Type> SupportedTypes { get; }
        public IEnumerable<Type> UnsupportedTypes { get; }
        public IInspectorExtensionVEBuilder VEBuilder => veBuilder ?? this;

        public SimpleInspectorExtension(
            InspectorExtensionLocation location,
            IInspectorExtensionVEBuilder veBuilder,
            Type[] supportedTypes,
            Type[] unsupportedTypes)
        {
            Location = location;
            this.veBuilder = veBuilder;
            SupportedTypes = supportedTypes;
            UnsupportedTypes = unsupportedTypes;
        }

        public SimpleInspectorExtension(
            InspectorExtensionLocation location,
            Func<InspectorExtensionContext, VisualElement> buildVEFunc,
            Type[] supportedTypes,
            Type[] unsupportedTypes)
        {
            this.buildVEFunc = buildVEFunc;

            Location = location;
            SupportedTypes = supportedTypes;
            UnsupportedTypes = unsupportedTypes;
        }

        public VisualElement BuildVE(InspectorExtensionContext context) => buildVEFunc(context);
    }
}
#endif
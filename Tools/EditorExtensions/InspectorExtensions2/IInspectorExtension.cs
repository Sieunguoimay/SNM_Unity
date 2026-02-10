#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public interface IInspectorExtension
    {
        InspectorExtensionLocation Location { get; }
        IEnumerable<Type> SupportedTypes { get; }
        IInspectorExtensionVEBuilder VEBuilder { get; }
    }

    public enum InspectorExtensionLocation
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public interface IInspectorExtensionVEBuilder
    {
        VisualElement BuildVE(InspectorExtensionContext context);
    }

    public class InspectorExtension : IInspectorExtension, IInspectorExtensionVEBuilder
    {
        private readonly Func<InspectorExtensionContext, VisualElement> buildVEFunc;
        private readonly IInspectorExtensionVEBuilder veBuilder;

        public InspectorExtensionLocation Location { get; }
        public IEnumerable<Type> SupportedTypes { get; }
        public IInspectorExtensionVEBuilder VEBuilder => veBuilder ?? this;

        public InspectorExtension(
            InspectorExtensionLocation location,
            IInspectorExtensionVEBuilder veBuilder,
            Type[] supportedTypes)
        {
            Location = location;
            this.veBuilder = veBuilder;
            SupportedTypes = supportedTypes;
        }

        public InspectorExtension(
            InspectorExtensionLocation location,
            Func<InspectorExtensionContext, VisualElement> buildVEFunc,
            Type[] supportedTypes)
        {
            this.buildVEFunc = buildVEFunc;

            Location = location;
            SupportedTypes = supportedTypes;
        }

        public VisualElement BuildVE(InspectorExtensionContext context) => buildVEFunc(context);
    }
}
#endif
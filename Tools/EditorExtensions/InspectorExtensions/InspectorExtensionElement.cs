#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class InspectorExtensionElement : VisualElement
    {
        private readonly object target;
        private readonly System.Attribute attribute;
        private readonly IInspectorExtension extension;

        public object Target => target;
        public System.Attribute Attribute => attribute;
        public IInspectorExtension Extension => extension;

        public InspectorExtensionElement(object target, Attribute attribute, IInspectorExtension extension)
        {
            this.target = target;
            this.attribute = attribute;
            this.extension = extension;
        }
    }

    public class InspectorExtensionElement_MemberInfo : InspectorExtensionElement
    {
        private readonly MemberInfo memberInfo;
        public MemberInfo MemberInfo => memberInfo;

        public InspectorExtensionElement_MemberInfo(object target, MemberInfo memberInfo, Attribute attribute, IInspectorExtension extension)
            : base(target, attribute, extension)
        {
            this.memberInfo = memberInfo;
        }
    }

    public enum ExtensionPosition
    {
        Top,
        Bottom
    }
}

#endif
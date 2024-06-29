#if UNITY_EDITOR
using System.Reflection;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class InspectorExtensionElement : VisualElement
    {
        private readonly object _target;
        private readonly MemberInfo _memberInfo;
        private readonly System.Attribute _attribute;

        public object Target => _target;
        public MemberInfo MemberInfo => _memberInfo;
        public System.Attribute Attribute => _attribute;

        public InspectorExtensionElement(object target, MemberInfo memberInfo, System.Attribute attribute)
        {
            _target = target;
            _memberInfo = memberInfo;
            _attribute = attribute;
        }
    }
}

#endif
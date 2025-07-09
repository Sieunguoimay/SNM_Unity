#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Snm.Tools.InspectorExtra
{
    public class CreateVisualElementExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Attribute;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Bottom;
        int IInspectorExtension.Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target) => target is CreateVisualElementAttribute;
        void IInspectorExtension.CleanUpStaticData()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            if (extensionElement is InspectorExtensionElement_MemberInfo e)
            {
                (e.MemberInfo as MethodInfo)
                    .Invoke(extensionElement.Target, new object[] { extensionElement });
            }
        }
    }
}

#endif
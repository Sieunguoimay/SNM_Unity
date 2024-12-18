#if UNITY_EDITOR
using System;
using System.Reflection;

namespace InspectorExtensions
{
    public class CreateVisualElementExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Attribute;
        Type IInspectorExtension.TargetType => typeof(CreateVisualElementAttribute);

        void IInspectorExtension.CleanUp()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            (extensionElement.MemberInfo as MethodInfo)
                .Invoke(extensionElement.Target, new object[] { extensionElement });
        }
    }
}

#endif
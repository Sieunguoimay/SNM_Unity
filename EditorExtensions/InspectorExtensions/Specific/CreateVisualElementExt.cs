#if UNITY_EDITOR
using System;
using System.Reflection;

namespace InspectorExtensions
{
    public class CreateVisualElementExt : IInspectorExtension
    {
        public ExtensionType ExtensionType => ExtensionType.Attribute;
        public Type TargetType => typeof(CreateVisualElementAttribute);

        public void CleanUp()
        {
        }

        public void ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            (extensionElement.MemberInfo as MethodInfo)
                .Invoke(extensionElement.Target, new object[] { extensionElement });
        }
    }
}

#endif
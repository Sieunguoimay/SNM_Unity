#if UNITY_EDITOR

using System;

namespace InspectorExtensions
{
    public interface IInspectorExtension
    {
        ExtensionType ExtensionType { get; }
        Type TargetType { get; }
        void ModifyExtensionElement(InspectorExtensionElement extensionElement);
        void CleanUp();
    }

    public enum ExtensionType
    {
        Attribute,
        Object
    }
}

#endif
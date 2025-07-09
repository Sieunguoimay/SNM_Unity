#if UNITY_EDITOR

namespace Snm.Tools.InspectorExtra
{
    public interface IInspectorExtension
    {
        ExtensionType ExtensionType { get; }
        ExtensionPosition Position { get; }
        int Priority { get; }
        bool IsSupportedFor(object target);
        void ModifyExtensionElement(InspectorExtensionElement extensionElement);
        void CleanUpStaticData();
    }

    public enum ExtensionType
    {
        Attribute,
        Object
    }
}

#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InspectorExtensions
{
    public class EditorSecondHeaderExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Top;
        int IInspectorExtension.Priority => 0;

        bool IInspectorExtension.IsSupportedFor(object target)
        {
            if (target is AssetImporter) return false;
            if (target is Object) return true;
            return false;
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            if (extensionElement.Target is Object target)
            {
                extensionElement.Add(new EditorSecondHeaderVE(target));
            }
        }

        void IInspectorExtension.CleanUp()
        {
        }
    }
}

#endif
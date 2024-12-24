#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InspectorExtensions
{
    public class EditorSecondHeaderExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Top;
        int IInspectorExtension.Priority => 0;

        private readonly Dictionary<UnityEngine.Object, ObjectData> objectStates = new();

        private class ObjectData
        {
            public InspectorModeHelper inspectorModeHelper;
        }

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
                if (!objectStates.ContainsKey(target))
                {
                    var inspectorModeHelper = new InspectorModeHelper(extensionElement.Editor.serializedObject);
                    objectStates.Add(target, new ObjectData()
                    {
                        inspectorModeHelper = inspectorModeHelper,
                    });
                }
                extensionElement.Add(new EditorSecondHeaderVE(target, objectStates[target].inspectorModeHelper));
            }
        }

        void IInspectorExtension.CleanUp()
        {
        }
    }
}

#endif
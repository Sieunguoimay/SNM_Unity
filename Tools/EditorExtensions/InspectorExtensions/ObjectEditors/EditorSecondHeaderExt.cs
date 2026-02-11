#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtra
{
    public class EditorSecondHeaderExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Top;
        int IInspectorExtension.Priority => 0;

        private readonly Dictionary<Object, ObjectData> objectStates = new();

        private class ObjectData
        {
            public bool isDebugMode;
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
                IInspectorModeHelper inspectorModeHelper = null;

                if (target is MonoBehaviour)
                {
                    inspectorModeHelper = new InspectorModeHelper_DebugEditor(extensionElement);

                    if (!objectStates.ContainsKey(target))
                    {
                        objectStates.Add(target, new ObjectData()
                        {
                            isDebugMode = inspectorModeHelper.IsDebugMode(),
                        });
                    }

                    inspectorModeHelper.SetDebugMode(objectStates[target].isDebugMode ? InspectorMode.Debug : InspectorMode.Normal);
                    inspectorModeHelper.OnModeChanged += OnDebugModeChanged;
                }

                extensionElement.Insert(0, EditorSecondHeaderVECreator.Create(target, extensionElement.InspectorWindow, inspectorModeHelper));
                extensionElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
        }

        private void OnDebugModeChanged(IInspectorModeHelper helper)
        {
            objectStates[helper.Target].isDebugMode = helper.IsDebugMode();
        }

        void IInspectorExtension.CleanUpStaticData()
        {
            objectStates.Clear();
        }
    }
}

#endif
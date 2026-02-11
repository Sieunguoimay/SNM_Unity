#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionRenderer
    {
        private readonly InspectorWindowLayout[] windowLayouts;
        private readonly IInspectorExtensionFilter extensionFilter;
        private readonly List<VisualElement> createdVEs = new();

        public InspectorExtensionRenderer(
            InspectorWindowLayout[] windowLayouts,
            IInspectorExtensionFilter extensionFilter)
        {
            this.windowLayouts = windowLayouts;
            this.extensionFilter = extensionFilter;
        }

        public void ApplyTools(IInspectorTool[] inspectorTools)
        {
            foreach (var wVE in windowLayouts)
            {
                foreach (var it in inspectorTools)
                {
                    var parentVE = it.Location switch
                    {
                        InspectorExtensionLocation.Left => wVE.AttachmentZones.Left,
                        InspectorExtensionLocation.Right => wVE.AttachmentZones.Right,
                        InspectorExtensionLocation.Top => wVE.AttachmentZones.Top,
                        InspectorExtensionLocation.Bottom => wVE.AttachmentZones.Bottom,
                        _ => throw new System.NotImplementedException(),
                    };

                    var toolVE = it.BuildVE(wVE.InspectorWindow);
                    parentVE.Add(toolVE);
                    createdVEs.Add(toolVE);
                }
            }
        }

        public void ApplyExtensions(IInspectorExtension[] extensions)
        {
            foreach (var windowLayout in windowLayouts)
            {
                foreach (var editorLayout in windowLayout.EditorLayouts)
                {
                    foreach (var extension in extensions)
                    {
                        var context = new InspectorExtensionContext(editorLayout.TargetObjects, windowLayout.InspectorWindow, editorLayout.SerializedObject);

                        if (extensionFilter.IsMatch(extension, context))
                        {
                            var extVE = extension.VEBuilder.BuildVE(context);
                            var parentVE = extension.Location switch
                            {
                                InspectorExtensionLocation.Top => editorLayout.AttachmentZones.Top,
                                InspectorExtensionLocation.Bottom => editorLayout.AttachmentZones.Bottom,
                                InspectorExtensionLocation.Left => editorLayout.AttachmentZones.Left,
                                InspectorExtensionLocation.Right => editorLayout.AttachmentZones.Right,
                                _ => throw new System.NotImplementedException(),
                            };
                            parentVE.Add(extVE);
                            createdVEs.Add(extVE);
                        }
                    }
                }
            }
        }

        public void ClearVEs()
        {
            foreach (var eve in createdVEs)
            {
                eve.RemoveFromHierarchy();
            }
            createdVEs.Clear();
        }
    }
}
#endif
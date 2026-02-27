#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorToolRenderer
    {
        private readonly InspectorWindowLayout[] windowLayouts;
        private readonly List<VisualElement> createdVEs = new();

        public InspectorToolRenderer(InspectorWindowLayout[] windowLayouts)
        {
            this.windowLayouts = windowLayouts;
        }

        public void ApplyTools(IInspectorTool[] inspectorTools, InspectorExtensionCoordinator coordinator)
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

                    var toolVE = it.BuildVE(new(wVE.InspectorWindow, coordinator));
                    parentVE.Add(toolVE);
                    createdVEs.Add(toolVE);
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
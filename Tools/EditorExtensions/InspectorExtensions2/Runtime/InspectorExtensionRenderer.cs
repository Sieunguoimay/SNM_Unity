#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionRenderer
    {
        private readonly EditorLayout[] editorLayouts;
        private readonly IInspectorExtensionFilter extensionFilter;
        private readonly List<VisualElement> createdVEs = new();

        public InspectorExtensionRenderer(
            EditorLayout[] editorLayouts,
            IInspectorExtensionFilter extensionFilter)
        {
            this.editorLayouts = editorLayouts;
            this.extensionFilter = extensionFilter;
        }

        public void ApplyExtensions(IInspectorExtension[] extensions)
        {
            foreach (var editorLayout in editorLayouts)
            {
                foreach (var extension in extensions)
                {
                    var context = new InspectorExtensionContext(
                        editorLayout.TargetObjects,
                        editorLayout.SerializedObject,
                        editorLayout.InspectorElement);

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
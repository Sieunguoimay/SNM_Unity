#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorWindowLayout
    {
        public AttachmentZones AttachmentZones { get; }
        public EditorWindow InspectorWindow { get; }
        public EditorLayout[] EditorLayouts { get; }

        public InspectorWindowLayout(
            AttachmentZones attachmentZones,
            EditorLayout[] editorVEs,
            EditorWindow inspectorWindow)
        {
            AttachmentZones = attachmentZones;
            EditorLayouts = editorVEs;
            InspectorWindow = inspectorWindow;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorLayout
    {
        public AttachmentZones AttachmentZones { get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public SerializedObject SerializedObject { get; }
        public InspectorElement InspectorElement { get; }
        public EditorLayout(
            AttachmentZones attachmentZones,
            UnityEngine.Object[] targetObjects,
            SerializedObject serializedObject,
            InspectorElement inspectorElement)
        {
            AttachmentZones = attachmentZones;
            TargetObjects = targetObjects;
            SerializedObject = serializedObject;
            InspectorElement = inspectorElement;
        }
    }
}
#endif
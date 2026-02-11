#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorLayout
    {
        public AttachmentZones AttachmentZones { get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public SerializedObject SerializedObject{ get; }

        public EditorLayout(
            AttachmentZones attachmentZones,
            UnityEngine.Object[] targetObjects,
            SerializedObject serializedObject)
        {
            AttachmentZones = attachmentZones;
            TargetObjects = targetObjects;
            SerializedObject = serializedObject;
        }
    }
}
#endif
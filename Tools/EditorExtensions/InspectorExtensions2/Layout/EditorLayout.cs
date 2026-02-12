#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorLayout
    {
        public AttachmentZones AttachmentZones { get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public SerializedObject SerializedObject { get; }
        public IMGUIContainer IMGUIContainer { get; }
        public EditorLayout(
            AttachmentZones attachmentZones,
            UnityEngine.Object[] targetObjects,
            SerializedObject serializedObject,
            IMGUIContainer iMGUIContainer)
        {
            AttachmentZones = attachmentZones;
            TargetObjects = targetObjects;
            SerializedObject = serializedObject;
            IMGUIContainer = iMGUIContainer;
        }
    }
}
#endif
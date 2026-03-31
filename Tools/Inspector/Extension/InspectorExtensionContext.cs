#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorExtensionContext
    {
        public SerializedObject SerializedObject { get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public VisualElement InspectorElement { get; }

        internal InspectorExtensionContext(
            UnityEngine.Object[] targets,
            SerializedObject serializedObject,
            VisualElement inspectorElement)
        {
            TargetObjects = targets;
            SerializedObject = serializedObject;
            InspectorElement = inspectorElement;
        }
    }
}
#endif
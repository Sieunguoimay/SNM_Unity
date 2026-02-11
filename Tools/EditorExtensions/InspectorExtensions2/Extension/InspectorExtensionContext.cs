#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorExtensionContext
    {
        public SerializedObject SerializedObject{ get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public EditorWindow InspectorWindow { get; }

        internal InspectorExtensionContext(
            UnityEngine.Object[] targets,
            EditorWindow inspectorWindow,
            SerializedObject serializedObject)
        {
            TargetObjects = targets;
            InspectorWindow = inspectorWindow;
            SerializedObject = serializedObject;
        }
    }
}
#endif
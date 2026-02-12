#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorExtensionContext
    {
        public SerializedObject SerializedObject{ get; }
        public UnityEngine.Object[] TargetObjects { get; }
        public EditorWindow InspectorWindow { get; }
        public IMGUIContainer IMGUIContainer{get;}

        internal InspectorExtensionContext(
            UnityEngine.Object[] targets,
            EditorWindow inspectorWindow,
            SerializedObject serializedObject,
            IMGUIContainer iMGUIContainer)
        {
            TargetObjects = targets;
            InspectorWindow = inspectorWindow;
            SerializedObject = serializedObject;
            IMGUIContainer = iMGUIContainer;
        }
    }
}
#endif
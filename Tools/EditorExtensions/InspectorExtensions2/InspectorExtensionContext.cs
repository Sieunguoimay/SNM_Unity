#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorExtensionContext
    {
        public UnityEngine.Object Target { get; }
        public EditorWindow InspectorWindow { get; }

        internal InspectorExtensionContext(
            UnityEngine.Object target, 
            EditorWindow inspectorWindow)
        {
            Target = target;
            InspectorWindow = inspectorWindow;
        }
    }
}
#endif
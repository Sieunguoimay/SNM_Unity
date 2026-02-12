#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorToolContext
    {
        public EditorWindow InspectorWindow { get; }
        public InspectorExtensionCoordinator Coordinator { get; }
        public InspectorToolContext(
            EditorWindow inspectorWindow, 
            InspectorExtensionCoordinator coordinator)
        {
            InspectorWindow = inspectorWindow;
            Coordinator = coordinator;
        }
    }

    public interface IInspectorTool
    {
        InspectorExtensionLocation Location { get; }
        VisualElement BuildVE(InspectorToolContext context);
    }
}
#endif
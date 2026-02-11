#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public interface IInspectorTool
    {
        InspectorExtensionLocation Location { get; }
        VisualElement BuildVE(EditorWindow inspectorWindow);
    }
}
#endif
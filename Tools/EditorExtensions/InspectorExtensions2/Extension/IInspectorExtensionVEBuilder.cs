#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public interface IInspectorExtensionVEBuilder
    {
        VisualElement BuildVE(InspectorExtensionContext context);
    }
}
#endif
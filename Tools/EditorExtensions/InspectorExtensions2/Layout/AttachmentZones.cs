#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public readonly struct AttachmentZones
    {
        public VisualElement Top { get; }
        public VisualElement Bottom { get; }
        public VisualElement Left { get; }
        public VisualElement Right { get; }

        public AttachmentZones(
            VisualElement top,
            VisualElement bottom,
            VisualElement left,
            VisualElement right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
    }
}
#endif
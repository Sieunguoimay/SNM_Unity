#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public interface ICustomPropertyReference
    {
        bool Supports(SerializedProperty property);
        void HandleClick(SerializedProperty property);
    }
}
#endif
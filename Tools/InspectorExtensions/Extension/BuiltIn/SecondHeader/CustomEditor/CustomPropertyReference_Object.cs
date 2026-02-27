#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    [CustomPropertyReference]
    public class CustomPropertyReference_Object : ICustomPropertyReference
    {
        public bool Supports(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference
                && property.objectReferenceValue != null;
        }

        public void HandleClick(SerializedProperty property)
        {
            EditorPopupWindow.Open(property.objectReferenceValue);
        }
    }
}
#endif
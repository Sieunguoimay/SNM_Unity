#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using PropertyAttribute = UnityEngine.PropertyAttribute;

namespace Snm.PropertyAttributes
{
    public class DisableFieldAttribute : PropertyAttribute
    {
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DisableFieldAttribute))]
    public class DisableFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var globalEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = globalEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
#endif
}
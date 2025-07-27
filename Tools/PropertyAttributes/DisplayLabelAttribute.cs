using UnityEditor;
using UnityEngine;

public class DisplayLabelAttribute : PropertyAttribute
{
    public string Label { get; private set; }
    public DisplayLabelAttribute(string label)
    {
        Label = label;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DisplayLabelAttribute))]
public class DisplayLabelDrawer : PropertyDrawer
{
    private DisplayLabelAttribute _attr;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _attr ??= attribute as DisplayLabelAttribute;
        var newLabel = new GUIContent(_attr.Label);

        EditorGUI.BeginProperty(position, newLabel, property);
        EditorGUI.PropertyField(position, property, newLabel, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif

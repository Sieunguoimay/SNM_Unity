using Sieunguoimay.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Insert a new line of text below the serialize field in Inspector
/// </summary>
public class TextLineBelowAttribute : PropertyAttribute
{
    public string MemberName { get; private set; }
    public TextLineBelowAttribute(string memberName)
    {
        MemberName = memberName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TextLineBelowAttribute))]
public class DisplayBellowDrawer : PropertyDrawer
{
    TextLineBelowAttribute _attr;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _attr ??= (TextLineBelowAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);
        var propertyRect = new Rect(position.x, position.y, position.width, position.height - EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propertyRect, property, label, true);

        var extraLabelRect = new Rect(position.x, position.y + propertyRect.height, position.width, EditorGUIUtility.singleLineHeight);
        var content = SerializeUtility.GetSiblingProperty(property, _attr.MemberName) as string;
        var italicStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Italic
        };
        EditorGUI.LabelField(extraLabelRect, content, italicStyle);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var propertyHeight = EditorGUI.GetPropertyHeight(property);
        return propertyHeight + EditorGUIUtility.singleLineHeight;
    }
}
#endif

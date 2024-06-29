#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

public class InsertTextLinesBelowAttribute : PropertyAttribute
{
    public string PropertyName { get; private set; }

    /// <summary>
    /// String collections IEnumerable<string>
    /// </summary>
    /// <param name="propertyName"></param>
    public InsertTextLinesBelowAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(InsertTextLinesBelowAttribute))]
public class InsertTextLineBelowDrawer : PropertyDrawer
{
    private InsertTextLinesBelowAttribute _att;
    private InsertTextLinesBelowAttribute Attribute => _att ??= attribute as InsertTextLinesBelowAttribute;

    private IEnumerable<string> GetTextLines(SerializedProperty property)
    {
        var target = property.serializedObject.targetObject;
        var flags = System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static;
        var propertyInfo = target.GetType().GetProperty(Attribute.PropertyName, flags);
        if (propertyInfo == null)
        {
            return Enumerable.Empty<string>();
        }
        return propertyInfo.GetValue(target) as IEnumerable<string>;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var textLines = GetTextLines(property);
        position.height -= (EditorGUIUtility.singleLineHeight + 2) * textLines.Count();
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label);

        foreach (var textLine in textLines)
        {
            position.y += position.height;
            position.height = (EditorGUIUtility.singleLineHeight + 2);
            EditorGUI.LabelField(position, textLine);
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property) + (EditorGUIUtility.singleLineHeight + 2) * GetTextLines(property).Count();
    }
}
#endif

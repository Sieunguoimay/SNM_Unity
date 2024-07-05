#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

public class FieldButtonAttribute : PropertyAttribute
{
    public string OnClickMethod { get; private set; }

    public FieldButtonAttribute(string onClickMethod)
    {
        OnClickMethod = onClickMethod;
    }
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(FieldButtonAttribute))]
public class FieldButtonDrawer : PropertyDrawer
{
    private FieldButtonAttribute _att;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _att ??= attribute as FieldButtonAttribute;
        EditorGUI.BeginProperty(position, label, property);
        position.width -= 20;
        EditorGUI.PropertyField(position, property, label, true);
        position.x += position.width + 1;
        position.width = 18;
        if (GUI.Button(position, new GUIContent("!", $"Invoke {_att.OnClickMethod}()")))
        {
            var target = property.serializedObject.targetObject;
            var method = target.GetType().GetMethod(_att.OnClickMethod,
                System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(target, new object[] { });
        }
        EditorGUI.EndProperty();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif

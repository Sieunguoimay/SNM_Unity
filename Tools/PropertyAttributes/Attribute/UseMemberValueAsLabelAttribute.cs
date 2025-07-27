#if UNITY_EDITOR
using Sieunguoimay.Serialization;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

public class UseMemberValueAsLabelAttribute : PropertyAttribute
{
    public string propertyName;
    public bool relativeProp;
    public UseMemberValueAsLabelAttribute(string propertyName, bool relativeProp = false)
    {
        this.propertyName = propertyName;
        this.relativeProp = relativeProp;
    }
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(UseMemberValueAsLabelAttribute))]
public class ProductTypeVisualConfigItemDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (attribute is not UseMemberValueAsLabelAttribute att) return;
        var lb = "";
        if (att.relativeProp)
        {
            var obj = SerializeUtility.GetObjectOfProperty(property);
            var p = obj.GetType().GetProperty(att.propertyName);
            if (p is not null)
            {
                lb = p.GetValue(obj) as string;
            }
            else
            {
                var m = obj.GetType().GetMethod(att.propertyName);
                lb = m.Invoke(obj, null) as string;
            }
        }
        else
        {
            lb = (SerializeUtility.GetSiblingProperty(property, att.propertyName) as string);
        }
        EditorGUI.PropertyField(position, property, new GUIContent(lb), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }
}

#endif
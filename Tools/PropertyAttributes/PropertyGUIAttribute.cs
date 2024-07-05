using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class PropertyGUIAttribute : PropertyAttribute
{
    public string MethodName { get; private set; }
    public bool IsPropertyInRootObject { get; private set; }

    public PropertyGUIAttribute(string methodName, bool isPropertyInRootObject = false)
    {
        MethodName = methodName;
        IsPropertyInRootObject = isPropertyInRootObject;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(PropertyGUIAttribute))]
public class PropertyGUIDrawer : PropertyDrawer
{
    private BindingFlags Flag => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
    private PropertyGUIAttribute _att;
    private MethodInfo _methodInfo;
    private object _target;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        _att ??= attribute as PropertyGUIAttribute;
        _target ??= _att.IsPropertyInRootObject ? property.serializedObject.targetObject : GetDirectTargetObject(property);
        if (_methodInfo == null)
        {
            var t = _target.GetType();
            while (t != null)
            {
                _methodInfo = t.GetMethod(_att.MethodName, Flag);
                if (_methodInfo == null)
                {
                    t = t.BaseType;
                }
                else
                {
                    break;
                }
            }
        }
        _methodInfo?.Invoke(_target, null);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }

    private object GetDirectTargetObject(SerializedProperty property)
    {
        var pathComponents = property.propertyPath.Replace("Array.data[", "[").Split('.');
        var pathComponentsToDirectObject = pathComponents.Take(pathComponents.Length - 1);

        object currentObject = property.serializedObject.targetObject;

        var pattern = @"\[(\d+)\]";
        var regex = new Regex(pattern);
        foreach (var p in pathComponentsToDirectObject)
        {
            if (p.StartsWith('['))
            {
                if (int.TryParse(regex.Match(p).Groups[1].Value, out int arrIndex))
                {
                    currentObject = (currentObject as object[])[arrIndex];
                }
            }
            else
            {
                var t = currentObject.GetType();
                while (t != null)
                {
                    var fieldInfo = t.GetField(p, Flag);
                    if (fieldInfo != null)
                    {
                        currentObject = fieldInfo.GetValue(currentObject);
                        break;
                    }
                    else
                    {
                        t = t.BaseType;
                    }
                }
            }
        }

        return currentObject;
    }

}
#endif
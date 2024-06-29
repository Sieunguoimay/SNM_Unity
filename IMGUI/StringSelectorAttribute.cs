using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class StringSelectorAttribute : PropertyAttribute
{
    public string PropertyName { get; private set; }
    public bool IsPropertyInRootObject { get; private set; }
    public bool ShouldCacheStrings { get; private set; }

    public StringSelectorAttribute(string propertyName, bool isPropertyInRootObject = false, bool shouldCacheStrings = false)
    {
        PropertyName = propertyName;
        IsPropertyInRootObject = isPropertyInRootObject;
        ShouldCacheStrings = shouldCacheStrings;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StringSelectorAttribute))]
public class StringSelectorDrawer : PropertyDrawer
{
    private BindingFlags Flag => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
    private string[] _strings;
    private bool _valid;
    private bool _firstTime = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PrefixLabel(position, label);

        position.x += EditorGUIUtility.labelWidth;
        position.width -= EditorGUIUtility.labelWidth;

        if (_firstTime)
        {
            _strings = GetStrings(property).ToArray();
            _valid = _strings.Contains(property.stringValue);
        }

        var color = GUI.color;
        GUI.color = _valid ? color : Color.red;
        position.width -= 25;

        if (_strings.Length < 20)
        {
            if (EditorGUI.DropdownButton(position, new GUIContent(property.stringValue), FocusType.Passive))
            {
                ShowGenericMenu(_strings, property.stringValue, newValue =>
                {
                    property.serializedObject.Update();
                    property.stringValue = newValue;
                    property.serializedObject.ApplyModifiedProperties();
                    _valid = _strings.Contains(property.stringValue);
                });
            }
        }
        else
        {
            var newValue = EditorGUI.TextField(position, property.stringValue);
            if (newValue != property.stringValue)
            {
                property.serializedObject.Update();
                property.stringValue = newValue;
                property.serializedObject.ApplyModifiedProperties();
                _valid = _strings.Contains(property.stringValue);
            }
        }

        position.x += position.width;
        position.width = 25;
        var show = GUI.Button(position, "...");
        GUI.color = color;
        EditorGUI.EndProperty();

        if (show)
        {
            Tools.SearchWindow.Show(_strings, result =>
            {
                property.serializedObject.Update();
                property.stringValue = result;
                property.serializedObject.ApplyModifiedProperties();
                _valid = _strings.Contains(property.stringValue);
            });
        }
        _firstTime = false;
    }

    private IEnumerable<string> GetStrings(SerializedProperty property)
    {
        var att = attribute as StringSelectorAttribute;
        var shouldCreateNewStrings = !att.ShouldCacheStrings || (att.ShouldCacheStrings && _strings == null);

        if (shouldCreateNewStrings)
        {
            var target = att.IsPropertyInRootObject ? property.serializedObject.targetObject : GetDirectTargetObject(property);
            var propertyInfo = target.GetType().GetProperty(att.PropertyName, Flag);
            var strings = (propertyInfo?.GetValue(target) as IEnumerable<string>) ?? Enumerable.Empty<string>();

            _strings = strings.ToArray();
        }

        return _strings;
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
                currentObject = currentObject.GetType().GetField(p, Flag).GetValue(currentObject);
            }
        }

        return currentObject;
    }

    private void ShowGenericMenu(IEnumerable<string> strings, string currentValue, System.Action<string> onNewValue)
    {

        var menu = new GenericMenu();
        foreach (var i in strings)
        {
            menu.AddItem(new GUIContent(i), i == currentValue, () =>
            {
                onNewValue?.Invoke(i);
            });
        }
        menu.ShowAsContext();
    }
}
#endif
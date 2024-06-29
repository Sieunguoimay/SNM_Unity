using UnityEngine;
using PropertyExt;
using System.Reflection;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SubAssetSelectAttribute : MoreButtonAttribute
{
    private readonly Type typeConstraint;

    public SubAssetSelectAttribute(string getAssetPath, Type typeConstraint = null) : base(getAssetPath)
    {
        this.typeConstraint = typeConstraint;
    }

    public override void OnButtonClicked(SerializedProperty property)
    {
        var sibValue = InvokeMemberForValue(SiblingName, property);
        var path = sibValue is UnityEngine.Object obj ? AssetDatabase.GetAssetPath(obj) : (sibValue is string str ? str : "");
        if (!string.IsNullOrEmpty(path))
        {
            var menuItem = new GenericMenu();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path).Where(p => typeConstraint?.IsAssignableFrom(p.GetType()) ?? true))
            {
                menuItem.AddItem(new GUIContent(o.name), property.objectReferenceValue == o, () =>
                {
                    property.objectReferenceValue = o;
                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                });
            }
            menuItem.ShowAsContext();
        }
    }

    private static object InvokeMemberForValue(string assetPathGet, SerializedProperty property)
    {
        var obj = GetParentObjectOfProperty(property);
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
        var method = obj.GetType().GetMethod(assetPathGet, flags);
        var prop = obj.GetType().GetProperty(assetPathGet, flags);
        var field = obj.GetType().GetField(assetPathGet, flags);

        return method?.Invoke(obj, new object[] { }) ?? prop?.GetValue(obj) ?? field?.GetValue(obj);
    }
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SubAssetSelectAttribute))]
public class SubAssetSelectDrawer : MoreButtonDrawer { }
#endif
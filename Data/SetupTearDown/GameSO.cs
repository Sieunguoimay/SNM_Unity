using InspectorExtensions;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class GameSO : ScriptableObject, ISetupTearDown
{
    [SerializeField] private GameSO[] children;

    [NonSerialized]
    private bool _isSetup = false;

    public virtual IEnumerable<ISetupTearDown> GetChildren()
    {
        foreach (var c in children)
        {
            yield return c;
        }

        foreach (var c in GameFO.GetChildrenByReflection(this))
        {
            yield return c;
        }
    }

    public virtual void Setup()
    {
        foreach (var child in GetChildren())
        {
            child.Setup();
        }

        _isSetup = true;
    }

    public virtual void TearDown()
    {
        foreach (var child in GetChildren())
        {
            child.TearDown();
        }

        _isSetup = false;
    }

#if UNITY_EDITOR
    [IMGUIMethod]
    private void OnIMGUI()
    {
        EditorGUILayout.LabelField($"IsSetup: {_isSetup}");
        DrawChildren(this);
    }

    private void DrawChildren(ISetupTearDown current)
    {
        foreach (var c in current.GetChildren())
        {
            EditorGUILayout.LabelField(new GUIContent($"{c.GetType().Name}"));
            if (c is IOnIMGUI imgui)
            {
                imgui.OnIMGUI();
            }
            EditorGUI.indentLevel++;
            DrawChildren(c);
            EditorGUI.indentLevel--;
        }
    }
#endif
}

public interface IOnIMGUI
{
    void OnIMGUI();
}

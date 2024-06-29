#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace HierarchyExtensions
{

    // [InitializeOnLoad]
    public class HierarchyExtEntryPoint
    {
        static HierarchyExtEntryPoint()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (Selection.activeGameObject == obj)
            {
                selectionRect.x += selectionRect.width - 20;
                selectionRect.width = 20;
                var style = new GUIStyle(GUI.skin.button);
                if (GUI.Button(selectionRect, new GUIContent("+", "Create Not Empty"), style))
                {
                    new Tools.CreateGameObjectWithComponent(
                        new UnityEditor.IMGUI.Controls.AdvancedDropdownState(), obj as GameObject)
                        .Show(new Rect(selectionRect.x, selectionRect.y, 0, 0));
                }
            }
        }
    }
}

#endif
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorHeaderVECreator
    {
        public static VisualElement BuildVE(EditorWindow inspectorWindow, Action refreshAction)
        {
            var root = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

            var button_Debug = CreateDebugButton(new InspectorModeViewer_Window(inspectorWindow), refreshAction);
            var space = new VisualElement() { style = { flexGrow = 1 } };

            root.Add(space);
            root.Add(button_Debug);

            return root;
        }

        private static Button CreateDebugButton(InspectorModeViewer_Window debugMode, Action refreshAction)
        {
            Button button_Debug = null;
            button_Debug = new Button
            {
                text = debugMode.Mode == InspectorMode.Debug ? "Normal" : "Debug",
                clickable = new(() =>
                {
                    var inverted = debugMode.Mode == InspectorMode.Debug ? InspectorMode.Normal : InspectorMode.Debug;
                    debugMode.SetInspectorMode(inverted);
                    button_Debug.text = inverted == InspectorMode.Debug ? "Normal" : "Debug";
                    refreshAction?.Invoke();
                })
            };

            return button_Debug;
        }
    }
}
#endif
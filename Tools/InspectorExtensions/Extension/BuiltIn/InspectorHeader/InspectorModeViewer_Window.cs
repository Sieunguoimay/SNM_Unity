#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorModeViewer_Window
    {
        private readonly EditorWindow inspectorWindow;
        private readonly Type inspectorType;
        private readonly BindingFlags flags;
        private readonly PropertyInfo propInfo_InspectorMode;
        private readonly MethodInfo methodInfo_Repaint;

        public InspectorMode Mode => (InspectorMode)propInfo_InspectorMode.GetValue(inspectorWindow);

        public InspectorModeViewer_Window(EditorWindow inspectorWindow)
        {
            this.inspectorWindow = inspectorWindow;
            inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType.FullName != "UnityEditor.InspectorWindow")
            {
                Debug.LogError("This is not an inspector window");
                return;
            }

            flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            propInfo_InspectorMode = inspectorType.GetProperty("inspectorMode", flags);
            methodInfo_Repaint = inspectorType.GetMethod("Repaint", flags);
        }


        public void SetInspectorMode(InspectorMode mode)
        {
            propInfo_InspectorMode.SetValue(inspectorWindow, mode);
            methodInfo_Repaint?.Invoke(inspectorWindow, null);
        }
    }
}
#endif
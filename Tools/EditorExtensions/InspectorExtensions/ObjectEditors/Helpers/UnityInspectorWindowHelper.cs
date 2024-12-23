#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace InspectorExtensions
{
    public class UnityInspectorWindowHelper
    {
        private readonly EditorWindow inspectorWindow;
        private readonly Type inspectorType;
        private readonly PropertyInfo fieldInfo_InspectorMode;
        private readonly MethodInfo methodInfo_Repaint;

        public UnityInspectorWindowHelper(EditorWindow inspectorWindow)
        {
            this.inspectorWindow = inspectorWindow;
            inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType.FullName != "UnityEditor.InspectorWindow")
            {
                Debug.LogError("This is not an inspector window");
                return;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            fieldInfo_InspectorMode = inspectorType.GetProperty("inspectorMode", flags);
            methodInfo_Repaint = inspectorType.GetMethod("Repaint", flags);
        }

        public InspectorMode GetInspectorMode()
        {
            return (InspectorMode)fieldInfo_InspectorMode.GetValue(inspectorWindow);
        }

        public void SetInspectorMode(InspectorMode mode)
        {
            fieldInfo_InspectorMode.SetValue(inspectorWindow, mode);
            methodInfo_Repaint?.Invoke(inspectorWindow, null);
        }
    }
}

#endif
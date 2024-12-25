#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

namespace InspectorExtensions
{
    public class InspectorWindowHelper
    {
        private readonly EditorWindow inspectorWindow;
        private readonly Type inspectorType;
        private readonly BindingFlags flags;
        private readonly PropertyInfo fieldInfo_InspectorMode;
        private readonly MethodInfo methodInfo_Repaint;

        public InspectorWindowHelper(EditorWindow inspectorWindow)
        {
            this.inspectorWindow = inspectorWindow;
            inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType.FullName != "UnityEditor.InspectorWindow")
            {
                Debug.LogError("This is not an inspector window");
                return;
            }

            flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

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

        public UnityEngine.Object[] GetInspectedObjects()
        {
            return (UnityEngine.Object[])inspectorType.GetMethod("GetInspectedObjects", flags).Invoke(inspectorWindow, null);
        }
    }
}

#endif
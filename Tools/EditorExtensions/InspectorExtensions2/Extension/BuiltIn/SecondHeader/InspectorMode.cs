#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorMode
    {
        private readonly PropertyInfo propInfo_InspectorMode;
        private readonly SerializedObject serializedObject;

        public bool IsDebugMode => ((UnityEditor.InspectorMode)propInfo_InspectorMode.GetValue(serializedObject)) == UnityEditor.InspectorMode.Debug;

        public InspectorMode(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            propInfo_InspectorMode = typeof(SerializedObject)
                .GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public void SetDebugMode(UnityEditor.InspectorMode mode)
        {
            propInfo_InspectorMode.SetValue(serializedObject, mode);
        }

        // private static VisualElement CreateDebugButton(InspectorMode inspectorModeHelper)
        // {
        //     Button root = null;
        //     root = new Button()
        //     {
        //         text = inspectorModeHelper.IsDebugMode ? "Debug" : "Normal",
        //         clickable = new(() =>
        //         {
        //             var mode = inspectorModeHelper.IsDebugMode ? UnityEditor.InspectorMode.Normal : UnityEditor.InspectorMode.Debug;
        //             inspectorModeHelper.SetDebugMode(mode);
        //             root.text = inspectorModeHelper.IsDebugMode ? "Debug" : "Normal";
        //         }
        //     )
        //     };
        //     return root;
        // }
    }
}
#endif

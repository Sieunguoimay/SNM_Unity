#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorModeViewer
    {
        private readonly PropertyInfo propInfo_InspectorMode;
        private readonly SerializedObject serializedObject;

        public InspectorMode Mode => (InspectorMode)propInfo_InspectorMode.GetValue(serializedObject);

        public InspectorModeViewer(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            propInfo_InspectorMode = typeof(SerializedObject)
                .GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public void SetInspectorMode(InspectorMode mode)
        {
            propInfo_InspectorMode.SetValue(serializedObject, mode);
        }
    }
}
#endif

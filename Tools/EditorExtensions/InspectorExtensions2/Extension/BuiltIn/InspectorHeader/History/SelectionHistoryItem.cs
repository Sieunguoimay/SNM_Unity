#if UNITY_EDITOR

using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryItem
    {
        private readonly UnityEngine.Object target;

        public UnityEngine.Object Target => target;

        public bool IsCurrent => Selection.activeObject == target;

        public SelectionHistoryItem(UnityEngine.Object target)
        {
            this.target = target;
        }

        public void Select()
        {
            Selection.activeObject = target;
        }
    }
}
#endif
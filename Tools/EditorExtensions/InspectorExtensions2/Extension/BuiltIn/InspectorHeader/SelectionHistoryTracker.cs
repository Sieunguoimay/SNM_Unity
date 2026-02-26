#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryTracker : IDisposable
    {
        private readonly List<UnityEngine.Object> selectionHistory = new();

        public IReadOnlyList<UnityEngine.Object> SelectionHistory => selectionHistory;

        public event Action OnSelectionHistoryChanged;

        public SelectionHistoryTracker()
        {
            Selection.selectionChanged += Selection_OnSelectionChanged;
            CaptureHistroy();
        }

        public void Dispose()
        {
            Selection.selectionChanged -= Selection_OnSelectionChanged;
            selectionHistory.Clear();
        }

        private void Selection_OnSelectionChanged()
        {
            CaptureHistroy();
        }

        private void CaptureHistroy()
        {
            var current = Selection.activeObject;
            if (current != null)
            {
                if (!selectionHistory.Contains(current))
                {
                    selectionHistory.Add(current);

                    //limit the history to 3 items
                    if (selectionHistory.Count > 3)
                    {
                        selectionHistory.RemoveAt(0);
                    }
                    OnSelectionHistoryChanged?.Invoke();
                }
            }
        }
    }
}
#endif
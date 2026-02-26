#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryPanelVEPresenter : IDisposable
    {
        private readonly SelectionHistoryTracker tracker;
        private readonly VisualElement itemVEContainer;

        public SelectionHistoryPanelVEPresenter(
            SelectionHistoryTracker tracker,
            VisualElement itemVEContainer)
        {

            this.tracker = tracker;
            this.itemVEContainer = itemVEContainer;

            this.tracker.OnSelectionHistoryChanged += Tracker_OnSelectionHistoryChanged;

            UpdateItemVEs();
        }

        public void Dispose()
        {
            this.tracker.OnSelectionHistoryChanged -= Tracker_OnSelectionHistoryChanged;
            itemVEContainer.Clear();
        }

        private void Tracker_OnSelectionHistoryChanged()
        {
            UpdateItemVEs();
        }

        private void UpdateItemVEs()
        {
            itemVEContainer.Clear();

            var displayCount = Mathf.Min(tracker.SelectionHistory.Count, 3);

            for (int i = displayCount - 1; i >= 0; i--)
            {
                var index = tracker.SelectionHistory.Count - 1 - i;
                var item = new SelectionHistoryItem(tracker.SelectionHistory[index]);
                var itemVE = SelectionHistoryItemVECreator.BuildVE(item);
                itemVEContainer.Add(itemVE);
            }
        }
    }

    public class SelectionHistoryItemVECreator
    {
        public static VisualElement BuildVE(SelectionHistoryItem item)
        {
            var root = new VisualElement();
            var objectField = new ObjectField
            {
                value = item.Target,
                objectType = typeof(UnityEngine.Object),
            };
            objectField.SetEnabled(!item.IsCurrent);
            objectField.RegisterCallback<MouseDownEvent>(_ => item.Select());

            root.Add(objectField);
            return root;
        }
    }

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
#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
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
            var historyButton = new Button()
            {
                clickable = new(ClickHistoryButton),
                style = { width = 20, marginRight = 0, marginLeft = 0, backgroundImage = EditorGUIUtility.IconContent("align_vertically_center_active").image as Texture2D },
                tooltip = "History"
            };

            itemVEContainer.Add(historyButton);

            foreach (var item in tracker.QuickAccessHistory)
            {
                var itemVE = SelectionHistoryItemVECreator.BuildVE(new SelectionHistoryItem(item));
                itemVEContainer.Add(itemVE);
            }
        }

        public void ClickHistoryButton()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Quick Access Count/3"), tracker.MaxQuickAccessHistoryCount == 3, () => ChangeMaxQuickAccessHistoryCount(3));
            menu.AddItem(new GUIContent("Quick Access Count/4"), tracker.MaxQuickAccessHistoryCount == 4, () => ChangeMaxQuickAccessHistoryCount(4));
            menu.AddItem(new GUIContent("Quick Access Count/5"), tracker.MaxQuickAccessHistoryCount == 5, () => ChangeMaxQuickAccessHistoryCount(5));
            menu.AddItem(new GUIContent("All History"), false, OpenSearchWindow);
            menu.AddItem(new GUIContent("Clear"), false, tracker.ClearHistory);
            menu.ShowAsContext();
        }

        private void ChangeMaxQuickAccessHistoryCount(int count)
        {
            tracker.SetMaxQuickAccessHistoryCount(count);
            UpdateItemVEs();
        }

        public void OpenSearchWindow()
        {
            var dic = tracker.AllHistory
                .Reverse()
                .ToDictionary(h => h.name + " (" + h.GetType().Name + ") " + h.GetInstanceID(), h => h);
            SearchWindow.Show(dic.Keys, str =>
            {
                var selected = dic[str];
                Selection.activeObject = selected;
            });
        }
    }

    public class SelectionHistoryItemVECreator
    {
        public static VisualElement BuildVE(SelectionHistoryItem item)
        {
            var root = new VisualElement();

            var button = new Button(() => item.Select()) { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, height = 20, marginLeft = 0, marginRight = 0 } };
            var content = EditorGUIUtility.ObjectContent(item.Target, item.Target?.GetType());
            var icon = new Image { image = content.image, style = { flexShrink = 0, width = 16, height = 16 } };
            var label = new Label(item.Target.name) { style = { unityTextAlign = TextAnchor.MiddleLeft, flexShrink = 1 }, tooltip = $"{item.Target.name} ({item.Target.GetType().Name})", };

            button.Add(icon);
            button.Add(label);

            button.SetEnabled(!item.IsCurrent);

            root.Add(button);
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
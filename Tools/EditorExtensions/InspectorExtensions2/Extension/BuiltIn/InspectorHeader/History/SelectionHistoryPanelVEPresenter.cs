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
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open InspectorExtension Script"), false, () => AssetDatabase.OpenAsset(AssetDatabase.FindAssets($"t:MonoScript {nameof(InspectorExtensionSystemEntrypoint)}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MonoScript>).FirstOrDefault()));
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
}
#endif
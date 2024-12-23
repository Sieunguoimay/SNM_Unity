#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static InspectorExtensions.InspectorExtensionHeaderVE;

namespace InspectorExtensions
{
    public class HistoryBrowserVE : VisualElement, IDisposable
    {
        private readonly IHistoryBrowser browsingData;
        private readonly IHistoryList historyList;
        private readonly VisualElement prevButton;
        private readonly VisualElement nextButton;
        private readonly HoverButton historyButton;

        public HistoryBrowserVE()
        {
            var data = new HistoryBrowsingData();
            historyList = data;
            browsingData = data;

            var historyContainer = this;
            historyContainer.style.flexDirection = FlexDirection.Row;

            prevButton = new HoverButton() { tooltip = "Prev" };
            prevButton.style.width = 20;
            prevButton.style.marginRight = 5;
            prevButton.style.marginLeft = 5;
            prevButton.style.backgroundImage = EditorGUIUtility.IconContent("d_tab_prev@2x").image as Texture2D;
            prevButton.RegisterCallback<ClickEvent>(OnPrevButtonClicked);
            historyContainer.Add(prevButton);

            nextButton = new HoverButton() { tooltip = "Next" };
            nextButton.style.width = 20;
            nextButton.style.marginRight = 5;
            nextButton.style.marginLeft = 5;
            nextButton.style.backgroundImage = EditorGUIUtility.IconContent("d_tab_next@2x").image as Texture2D;
            nextButton.RegisterCallback<ClickEvent>(OnNextButtonClicked);
            historyContainer.Add(nextButton);

            historyButton = new HoverButton() { tooltip = "History" };
            historyButton.style.width = 20;
            historyButton.style.marginRight = 5;
            historyButton.style.marginLeft = 5;
            historyButton.style.backgroundImage = EditorGUIUtility.IconContent("align_vertically_center_active").image as Texture2D;
            historyButton.RegisterCallback<ClickEvent>(OnShowHistoryButtonClicked);
            historyContainer.Add(historyButton);

            browsingData.OnHistoryChanged += OnHistoryChanged;
            browsingData.SetEnabled(true);

            UpdateHistoryNavigationButtons();
        }

        public void Dispose()
        {
            browsingData.OnHistoryChanged -= OnHistoryChanged;
            if (browsingData is IDisposable d)
            {
                d.Dispose();
            }
        }

        public void SetActive(bool status)
        {
            browsingData.SetEnabled(status);
            style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnShowHistoryButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            var history = historyList.GetHistoryDisplay().Distinct().ToList();
            for (int i = history.Count - 1; i >= 0; i--)
            {
                menu.AddItem(new GUIContent($"{history.Count - i}. {history[i]}"), historyList.IsHistoryCurrent(i), selected =>
                {
                    Selection.activeObject = historyList.GetObjectFromHistory((int)selected);
                }, i);
            }

            if (history.Count > 0)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Clear History"), false, () =>
                {
                    browsingData.ClearHistory();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Start browsing to get history"));
            }
            menu.ShowAsContext();
        }

        private void OnHistoryChanged(HistoryBrowsingData history)
        {
            UpdateHistoryNavigationButtons();
        }

        private void UpdateHistoryNavigationButtons()
        {
            prevButton.pickingMode = browsingData.CanNavigateBack ? PickingMode.Position : PickingMode.Ignore;
            prevButton.style.opacity = browsingData.CanNavigateBack ? 1 : 0.25f;
            nextButton.style.display = browsingData.CanNavigateForward ? DisplayStyle.Flex : DisplayStyle.None;
            historyButton.style.display = historyList.HistoryCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            prevButton.tooltip = browsingData.CanNavigateBack ? browsingData.Next : "";
            nextButton.tooltip = browsingData.CanNavigateForward ? browsingData.Prev : "";
        }

        private void OnPrevButtonClicked(ClickEvent evt)
        {
            browsingData.Navigate(-1);
        }

        private void OnNextButtonClicked(ClickEvent evt)
        {
            browsingData.Navigate(1);
        }

        public IEnumerable<IHeaderMenuItem> GetMenuItems()
        {
            return Enumerable.Empty<IHeaderMenuItem>();
        }
    }
}

#endif
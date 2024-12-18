#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class InspectorExtensionHeader : VisualElement
    {
        private IExtensionElementProvider _extensionElementProvider;
        private readonly ToggleButton toggleButton;
        private readonly VisualElement prevButton;
        private readonly VisualElement nextButton;
        private readonly BrowsingHistory browsingHistory = new();
        private readonly HoverButton historyButton;

        public InspectorExtensionHeader()
        {
            style.flexDirection = FlexDirection.RowReverse;
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(.1f, .1f, .1f, 1f);

            var refreshButton = new VisualElement() { tooltip = "Refresh" };
            refreshButton.style.width = 15;
            refreshButton.style.marginRight = 5;
            refreshButton.style.marginLeft = 5;
            refreshButton.style.backgroundImage = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;
            refreshButton.RegisterCallback<ClickEvent>(OnRefreshButtonClicked);
            Add(refreshButton);

            toggleButton = new ToggleButton("ON", "OFF", true, UpdateExtensionElementsVisible) { tooltip = GetTooltipText() };
            toggleButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(toggleButton);

            var space = new VisualElement();
            space.style.flexGrow = 1;
            Add(space);

            historyButton = new HoverButton() { tooltip = "History" };
            historyButton.style.width = 20;
            historyButton.style.marginRight = 5;
            historyButton.style.marginLeft = 5;
            historyButton.style.backgroundImage = EditorGUIUtility.IconContent("align_vertically_center_active").image as Texture2D;
            historyButton.RegisterCallback<ClickEvent>(OnShowHistoryButtonClicked);
            Add(historyButton);

            nextButton = new HoverButton() { tooltip = "Next" };
            nextButton.style.width = 20;
            nextButton.style.marginRight = 5;
            nextButton.style.marginLeft = 5;
            nextButton.style.backgroundImage = EditorGUIUtility.IconContent("d_tab_next@2x").image as Texture2D;
            nextButton.RegisterCallback<ClickEvent>(OnNextButtonClicked);
            Add(nextButton);

            prevButton = new HoverButton() { tooltip = "Prev" };
            prevButton.style.width = 20;
            prevButton.style.marginRight = 5;
            prevButton.style.marginLeft = 5;
            prevButton.style.backgroundImage = EditorGUIUtility.IconContent("d_tab_prev@2x").image as Texture2D;
            prevButton.RegisterCallback<ClickEvent>(OnPrevButtonClicked);
            Add(prevButton);

            var pingButton = new Button(() => EditorGUIUtility.PingObject(Selection.activeObject))
            {
                text = "Ping"
            };
            pingButton.style.marginTop = 0;
            pingButton.style.marginLeft = 0;
            pingButton.style.marginBottom = 0;
            Add(pingButton);

            browsingHistory.OnHistoryChanged += OnHistoryChanged;
            browsingHistory.SetEnabled(true);
            UpdateHistoryNavigationButtons();
        }

        private void OnShowHistoryButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            var history = browsingHistory.History.ToList();
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var index = i;
                var hItem = history[i];
                menu.AddItem(new GUIContent($"{history.Count - i}. {hItem}"), browsingHistory.Current == hItem, selected =>
                {
                    Selection.activeObject = browsingHistory.GetObjectFromHistory((int)selected);
                }, i);
            }

            if (history.Count > 0)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Clear History"), false, () =>
                {
                    browsingHistory.ClearHistory();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Start browsing to get history"));
            }
            menu.ShowAsContext();
        }

        private void OnHistoryChanged(BrowsingHistory history)
        {
            UpdateHistoryNavigationButtons();
        }

        private void UpdateHistoryNavigationButtons()
        {
            prevButton.pickingMode = browsingHistory.CanNavigateBack ? PickingMode.Position : PickingMode.Ignore;
            prevButton.style.opacity = browsingHistory.CanNavigateBack ? 1 : 0.25f;
            nextButton.style.display = browsingHistory.CanNavigateForward ? DisplayStyle.Flex : DisplayStyle.None;
            historyButton.style.display = browsingHistory.HistoryCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            prevButton.tooltip = browsingHistory.CanNavigateBack ? browsingHistory.Next : "";
            nextButton.tooltip = browsingHistory.CanNavigateForward ? browsingHistory.Prev : "";
        }

        private void OnPrevButtonClicked(ClickEvent evt)
        {
            browsingHistory.Navigate(-1);
        }

        private void OnNextButtonClicked(ClickEvent evt)
        {
            browsingHistory.Navigate(1);
        }

        private string GetTooltipText()
        {
            return "Inspector Extensions for: \n" + string.Join("\n", InspectorExtensionInstaller.Instance.InspectorExtensions.Select(e => $"{e.TargetType.Name}"));
        }

        private void OnRefreshButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                InspectorExtensionInstaller.Instance.TryModify();
            });
            menu.ShowAsContext();
        }

        private void UpdateExtensionElementsVisible()
        {
            if (_extensionElementProvider == null) return;

            var status = toggleButton.Status;
            foreach (var e in _extensionElementProvider.GetExtensionElements())
            {
                e.style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
            }

            browsingHistory.SetEnabled(status);
            historyButton.style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
            nextButton.style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
            prevButton.style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetExtensionElementProvider(IExtensionElementProvider extensionElementProvider)
        {
            _extensionElementProvider = extensionElementProvider;
            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
            _extensionElementProvider.OnExtensionElementsChanged += OnExtensionElementsChanged;
            UpdateExtensionElementsVisible();
        }

        private void OnExtensionElementsChanged(IExtensionElementProvider provider)
        {
            UpdateExtensionElementsVisible();
        }

        private class HoverButton : VisualElement
        {
            private readonly Color defaultBGColor;

            public HoverButton()
            {
                defaultBGColor = style.backgroundColor.value;
                RegisterCallback<MouseEnterEvent>(evt =>
                {
                    style.backgroundColor = new Color(.2f, .2f, .2f, 1f);
                });

                RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    style.backgroundColor = defaultBGColor;
                });
            }
        }
        private class ToggleButton : Label
        {
            private readonly string textOn;
            private readonly string textOff;
            private bool status = false;
            public bool Status => status;
            private readonly Action changed;

            public ToggleButton(string textOn, string textOff, bool initialStatus, Action changed)
            {
                this.textOn = textOn;
                this.textOff = textOff;
                this.changed = changed;
                status = initialStatus;
                text = status ? this.textOn : this.textOff;
                style.backgroundColor = new StyleColor() { value = status ? Color.green : Color.black };
                RegisterCallback<ClickEvent>(OnClick);

                RegisterCallback<MouseEnterEvent>(evt =>
                {
                    var color = status ? Color.green : Color.black;
                    style.backgroundColor = color * .75f;
                });

                RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    var color = status ? Color.green : Color.black;
                    style.backgroundColor = color;
                });
            }

            private void OnClick(ClickEvent evt)
            {
                status = !status;
                text = status ? textOn : textOff;
                style.backgroundColor = new StyleColor() { value = status ? Color.green : Color.black };
                changed?.Invoke();
            }
        }
    }

    public interface IExtensionElementProvider
    {
        IEnumerable<InspectorExtensionElement> GetExtensionElements();
        event Action<IExtensionElementProvider> OnExtensionElementsChanged;
    }
}

#endif
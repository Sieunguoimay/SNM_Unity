#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{

    public class InspectorExtensionHeaderVE : VisualElement, IDisposable
    {
        private readonly IExtensionElementProvider _extensionElementProvider;
        private readonly ToggleButton toggleButton;
        private readonly ToggleButton inspectorModeToggleButton;
        private readonly HistoryBrowserVE historyBrowser;
        private readonly UnityInspectorWindowHelper inspectorWindow;

        public InspectorExtensionHeaderVE(IExtensionElementProvider extensionElementProvider)
        {
            _extensionElementProvider = extensionElementProvider;

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

            Add(toggleButton = new ToggleButton(
                "ON", "OFF", Color.green, Color.black,
                null,
                UpdateExtensionElementsVisible,
                "InspectorExtensions_ToggleButton_Status")
            {
                tooltip = GetTooltipText()
            });

            var window = InspectorExtensionInstaller.Instance.InspectorWindow;
            if (window != null)
            {
                inspectorWindow = new UnityInspectorWindowHelper(window);
                if (inspectorWindow != null)
                {
                    Add(inspectorModeToggleButton = new ToggleButton(
                        "Debug", "Normal", Color.cyan * .8f, Color.black * .3f,
                        () => inspectorWindow.GetInspectorMode() == InspectorMode.Debug,
                        OnInspectorModeToggleButtonClicked,
                        "InspectorExtensions_ToggleButton_InspectorMode"));
                    inspectorModeToggleButton.style.marginRight = 3;
                    inspectorModeToggleButton.style.paddingLeft = 3;
                    inspectorModeToggleButton.style.paddingRight = 3;
                }
            }

            var space = new VisualElement();
            space.style.flexGrow = 1;
            Add(space);

            Add(historyBrowser = new());

            var pingButton = new Button(() => EditorGUIUtility.PingObject(Selection.activeObject))
            {
                text = "Ping"
            };
            pingButton.style.marginTop = 0;
            pingButton.style.marginLeft = 0;
            pingButton.style.marginBottom = 0;
            Add(pingButton);

            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
            _extensionElementProvider.OnExtensionElementsChanged += OnExtensionElementsChanged;

            UpdateExtensionElementsVisible();
        }

        private void OnInspectorModeToggleButtonClicked()
        {
            inspectorWindow.SetInspectorMode(inspectorWindow.GetInspectorMode() == InspectorMode.Normal ? InspectorMode.Debug : InspectorMode.Normal);
            InspectorExtensionInstaller.Instance.Refresh();
        }

        public void Dispose()
        {
            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
        }

        private string GetTooltipText()
        {
            return "No tooltip!";//Inspector Extensions for: \n" + string.Join("\n", InspectorExtensionInstaller.Instance.InspectorExtensions.Select(e => $"{e.TargetType.Name}"));
        }

        private void OnRefreshButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                InspectorExtensionInstaller.Instance.Refresh();
            });
            menu.AddItem(new GUIContent($"Open script {nameof(InspectorExtensionHeaderVE)}"), false, () =>
            {
                AssetDatabase.OpenAsset(
                    AssetDatabase.FindAssets($"t:MonoScript {nameof(InspectorExtensionHeaderVE)}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                    .FirstOrDefault());
            });

            var extraMenuItems = historyBrowser.GetMenuItems();

            foreach (var e in extraMenuItems)
            {
                menu.AddItem(new GUIContent($"{e.Category}/{e.DisplayName}"), e.IsActive, ee =>
                {
                    if (ee is IHeaderMenuItem i)
                    {
                        i.SetActive(!i.IsActive);
                    }
                }, e);
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Toggle Extension Debug"), InspectorExtensionInstaller.Instance.DebugEnabled, () =>
            {
                InspectorExtensionInstaller.Instance.DebugEnabled = !InspectorExtensionInstaller.Instance.DebugEnabled;
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
            historyBrowser.SetActive(status);
        }

        private void OnExtensionElementsChanged(IExtensionElementProvider provider)
        {
            UpdateExtensionElementsVisible();
        }

        public class HoverButton : VisualElement
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
            private readonly string saveKey;
            private readonly string textOn;
            private readonly string textOff;
            private readonly Color colorOn = Color.green;
            private readonly Color colorOff = Color.green;
            private readonly Func<bool> status;
            private bool StatusInternal
            {
                get => EditorPrefs.GetBool(saveKey, true);
                set => EditorPrefs.SetBool(saveKey, value);
            }
            public bool Status => StatusInternal;
            private readonly Action onClicked;

            public ToggleButton(string textOn, string textOff, Color colorOn, Color colorOff, Func<bool> status, Action onClicked, string saveKey)
            {
                this.textOn = textOn;
                this.textOff = textOff;
                this.colorOn = colorOn;
                this.colorOff = colorOff;
                this.onClicked = onClicked;
                this.saveKey = saveKey;
                this.status = status;
                SetInternalStatus(status?.Invoke() ?? StatusInternal);
                // text = StatusInternal ? this.textOn : this.textOff;
                // style.backgroundColor = new StyleColor() { value = StatusInternal ? colorOn : colorOff };
                style.unityTextAlign = TextAnchor.MiddleCenter;
                RegisterCallback<ClickEvent>(OnClick);

                RegisterCallback<MouseEnterEvent>(evt =>
                {
                    // var color = StatusInternal ? colorOn : colorOff;
                    // style.backgroundColor = color * .75f;
                    SetInternalStatus(status?.Invoke() ?? StatusInternal);
                    style.backgroundColor = colorOn * .8f;
                });

                RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    var color = StatusInternal ? colorOn : colorOff;
                    style.backgroundColor = color;
                });

                InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnRepaint);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            private void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnRepaint);
            }

            private void OnRepaint(MouseEnterEvent evt)
            {
                SetInternalStatus(status?.Invoke() ?? StatusInternal);
            }

            private void OnClick(ClickEvent evt)
            {
                SetInternalStatus(!StatusInternal);
                onClicked?.Invoke();
            }

            public void SetInternalStatus(bool status)
            {
                StatusInternal = status;
                text = StatusInternal ? textOn : textOff;
                style.backgroundColor = new StyleColor() { value = StatusInternal ? colorOn : colorOff };
            }
        }
    }

    public interface IExtensionElementProvider
    {
        IEnumerable<InspectorExtensionElement> GetExtensionElements();
        event Action<IExtensionElementProvider> OnExtensionElementsChanged;
    }

    public interface IHeaderMenuItem
    {
        string Category { get; }
        string DisplayName { get; }
        bool IsActive { get; }
        void SetActive(bool active);
    }
}

#endif
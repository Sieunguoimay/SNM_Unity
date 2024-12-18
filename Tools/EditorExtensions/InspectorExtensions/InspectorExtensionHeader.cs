#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{

    public class InspectorExtensionHeader : VisualElement, IDisposable
    {
        private readonly IExtensionElementProvider _extensionElementProvider;
        private readonly ToggleButton toggleButton;
        private readonly HistoryBrowserController historyBrowser;

        public InspectorExtensionHeader(IExtensionElementProvider extensionElementProvider)
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

            toggleButton = new ToggleButton("ON", "OFF", UpdateExtensionElementsVisible) { tooltip = GetTooltipText() };
            toggleButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(toggleButton);

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

        public void Dispose()
        {
            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
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

            menu.AddItem(new GUIContent($"Open script {nameof(InspectorExtensionHeader)}"), false, () =>
            {
                AssetDatabase.OpenAsset(
                    AssetDatabase.FindAssets($"t:MonoScript {nameof(InspectorExtensionHeader)}")
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
            private static string STATUS_KEY = "InspectorExtensions_ToggleButton_Status";
            private readonly string textOn;
            private readonly string textOff;
            private bool StatusInternal
            {
                get => EditorPrefs.GetBool(STATUS_KEY, true);
                set => EditorPrefs.SetBool(STATUS_KEY, value);
            }
            public bool Status => StatusInternal;
            private readonly Action changed;

            public ToggleButton(string textOn, string textOff, Action changed)
            {
                this.textOn = textOn;
                this.textOff = textOff;
                this.changed = changed;
                text = StatusInternal ? this.textOn : this.textOff;
                style.backgroundColor = new StyleColor() { value = StatusInternal ? Color.green : Color.black };
                RegisterCallback<ClickEvent>(OnClick);

                RegisterCallback<MouseEnterEvent>(evt =>
                {
                    var color = StatusInternal ? Color.green : Color.black;
                    style.backgroundColor = color * .75f;
                });

                RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    var color = StatusInternal ? Color.green : Color.black;
                    style.backgroundColor = color;
                });
            }

            private void OnClick(ClickEvent evt)
            {
                StatusInternal = !StatusInternal;
                text = StatusInternal ? textOn : textOff;
                style.backgroundColor = new StyleColor() { value = StatusInternal ? Color.green : Color.black };
                changed?.Invoke();
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
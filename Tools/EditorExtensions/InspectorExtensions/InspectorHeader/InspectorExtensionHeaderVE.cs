#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtra
{

    public class InspectorExtensionHeaderVE : VisualElement, IDisposable
    {
        private readonly IExtensionElementProvider _extensionElementProvider;
        private readonly IRefreshHandler refreshHandler;
        private readonly ToggleButton toggleButton;
        private readonly HistoryBrowserVE historyBrowser;
        private readonly InspectorWindowHelper inspectorWindowHelper;
        private readonly ToggleButton2 inspectorModeToggleButton;

        public InspectorExtensionHeaderVE(IExtensionElementProvider extensionElementProvider, EditorWindow window, IRefreshHandler refreshHandler)
        {
            _extensionElementProvider = extensionElementProvider;
            this.refreshHandler = refreshHandler;
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
                "InspectorExtensions_ToggleButton_Status",
                window)
            {
                tooltip = GetTooltipText()
            });
            toggleButton.style.height = 20;

            if (window != null)
            {
                inspectorWindowHelper = new InspectorWindowHelper(window);
                if (inspectorWindowHelper != null)
                {
                    Add(inspectorModeToggleButton = new ToggleButton2(
                        "Normal", "Debug", Color.cyan * .8f,
                        () => inspectorWindowHelper.GetInspectorMode() == InspectorMode.Debug,
                        OnInspectorModeToggleButtonClicked,
                        "InspectorExtensions_ToggleButton_InspectorMode",
                        window));
                    inspectorModeToggleButton.style.marginRight = 3;
                    inspectorModeToggleButton.style.marginTop = 0;
                    inspectorModeToggleButton.style.marginLeft = 0;
                    inspectorModeToggleButton.style.marginBottom = 0;
                }
            }

            // pingButton = new Button(OnPingButtonClicked) { text = "Ping" };
            // pingButton.style.marginTop = 0;
            // pingButton.style.marginRight = 1;
            // pingButton.style.marginLeft = 0;
            // pingButton.style.marginBottom = 0;
            // Add(pingButton);

            if (inspectorWindowHelper != null)
            {
                var inspectedObjects = inspectorWindowHelper.GetInspectedObjects();
                if (inspectedObjects != null && inspectedObjects.Length == 1 && inspectedObjects[0] is GameObject go && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                {
                    Button prefabHierarchyButton;
                    Add(prefabHierarchyButton = new(OnHierachyButtonClicked)
                    {
#if UNITY_2023_2_OR_NEWER
                        iconImage = EditorGUIUtility.IconContent("icon dropdown@2x").image as Texture2D,
#endif
                        text = $"{go.name}",
                        tooltip = $"{AssetDatabase.GetAssetPath(go)}"
                    });
                }
            }

            var space = new VisualElement();
            space.style.flexGrow = 1;
            Add(space);

            Add(historyBrowser = new());

            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
            _extensionElementProvider.OnExtensionElementsChanged += OnExtensionElementsChanged;

            UpdateExtensionElementsVisible();
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnHierachyButtonClicked()
        {
            var menu = new GenericMenu();
            if (inspectorWindowHelper != null)
            {
                var inspectedObjects = inspectorWindowHelper.GetInspectedObjects();
                if (inspectedObjects.Length > 0 && inspectedObjects[0] is GameObject go)
                {
                    foreach (var p in GetHierarchy(go.name, go.transform.root.gameObject))
                    {
                        menu.AddItem(new GUIContent(p.path.Replace("/", "\u2215")), p.go == go, z =>
                        {
                            Selection.activeObject = z as UnityEngine.Object;
                        }, p.go);
                    }
                }
            }
            menu.ShowAsContext();
        }

        private IEnumerable<(string path, GameObject go)> GetHierarchy(string rootPath, GameObject root)
        {
            yield return (rootPath, root);
            foreach (Transform c in root.transform)
            {
                foreach (var p in GetHierarchy(rootPath + "/" + c.name, c.gameObject))
                {
                    yield return p;
                }
            }
        }

        // private void OnPingButtonClicked()
        // {
        //     foreach (var o in inspectorWindowHelper.GetInspectedObjects())
        //     {
        //         EditorGUIUtility.PingObject(o);
        //     }
        // }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispose();
        }

        public void Dispose()
        {
            _extensionElementProvider.OnExtensionElementsChanged -= OnExtensionElementsChanged;
            historyBrowser.Dispose();
        }

        private void OnInspectorModeToggleButtonClicked()
        {
            inspectorWindowHelper.SetInspectorMode(inspectorWindowHelper.GetInspectorMode() == InspectorMode.Normal ? InspectorMode.Debug : InspectorMode.Normal);
            refreshHandler.Refresh();
        }

        private string GetTooltipText()
        {
            return "Toggle the all Inspector Extensions";//Inspector Extensions for: \n" + string.Join("\n", InspectorExtensionInstaller.Instance.InspectorExtensions.Select(e => $"{e.TargetType.Name}"));
        }

        private void OnRefreshButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                refreshHandler.Refresh();
            });

            foreach (var (path, script) in GetScriptsToOpen())
            {
                menu.AddItem(new GUIContent($"{path}"), false, _e =>
                {
                    AssetDatabase.OpenAsset(
                        AssetDatabase.FindAssets($"t:MonoScript {_e}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                        .FirstOrDefault());
                }, script);
            }

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

        private static IEnumerable<(string path, string script)> GetScriptsToOpen()
        {
            yield return ($"Open script {nameof(InspectorExtensionEntryPoint)}.cs", nameof(InspectorExtensionEntryPoint));
            yield return ($"Open script {nameof(InspectorExtensionHeaderVE)}.cs", nameof(InspectorExtensionHeaderVE));

            foreach (var e in InspectorExtensionInstaller.Instance.InspectorExtensions)
            {
                yield return ($"Open Extension Scripts/{e.GetType().Name}.cs", e.GetType().Name);
            }
        }

        private void UpdateExtensionElementsVisible()
        {
            if (_extensionElementProvider == null) return;

            var status = toggleButton.Status;
            foreach (var e in _extensionElementProvider.GetExtensionElements().Concat(new VisualElement[] { inspectorModeToggleButton }))
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
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SelectionHistoryPanelVEPresenter : IDisposable
    {
        private readonly SelectionHistoryTracker tracker;
        private readonly VisualElement container;

        private const int BreadcrumbCount = 3;

        public SelectionHistoryPanelVEPresenter(
            SelectionHistoryTracker tracker,
            VisualElement container)
        {
            this.tracker = tracker;
            this.container = container;

            this.tracker.OnHistoryChanged += OnHistoryChanged;
            Rebuild();
        }

        public void Dispose()
        {
            tracker.OnHistoryChanged -= OnHistoryChanged;
            container.Clear();
        }

        private void OnHistoryChanged() => Rebuild();

        private void Rebuild()
        {
            container.Clear();

            // Back button
            var backBtn = new Button(() => tracker.GoBack())
            {
                text = "\u25C0",
                tooltip = "Back",
                style =
                {
                    width = 24,
                    marginLeft = 0, marginRight = 0,
                    paddingLeft = 0, paddingRight = 0,
                    fontSize = 10,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            backBtn.SetEnabled(tracker.CanGoBack);
            container.Add(backBtn);

            // Forward button
            var fwdBtn = new Button(() => tracker.GoForward())
            {
                text = "\u25B6",
                tooltip = "Forward",
                style =
                {
                    width = 24,
                    marginLeft = 0, marginRight = 2,
                    paddingLeft = 0, paddingRight = 0,
                    fontSize = 10,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            fwdBtn.SetEnabled(tracker.CanGoForward);
            container.Add(fwdBtn);

            // Breadcrumb trail
            if (tracker.History.Count == 0) return;

            var cursor = tracker.Cursor;
            var history = tracker.History;

            // Determine visible window centered on cursor
            var halfWindow = BreadcrumbCount / 2;
            var start = cursor - halfWindow;
            var end = start + BreadcrumbCount - 1;

            if (start < 0) { start = 0; end = Math.Min(BreadcrumbCount - 1, history.Count - 1); }
            if (end >= history.Count) { end = history.Count - 1; start = Math.Max(0, end - BreadcrumbCount + 1); }

            // Ellipsis at start
            if (start > 0)
            {
                container.Add(new Label("...")
                {
                    style =
                    {
                        color = new Color(0.5f, 0.5f, 0.5f),
                        unityTextAlign = TextAnchor.MiddleCenter,
                        width = 16,
                        marginLeft = 0, marginRight = 0,
                    }
                });
            }

            for (var i = start; i <= end; i++)
            {
                var obj = history[i];
                if (obj == null) continue;

                var isCurrent = i == cursor;
                var index = i;
                var content = EditorGUIUtility.ObjectContent(obj, obj.GetType());

                var btn = new Button(() => tracker.NavigateTo(index))
                {
                    tooltip = $"{obj.name} ({obj.GetType().Name})",
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        height = 20,
                        marginLeft = 0, marginRight = 0,
                        paddingLeft = 4, paddingRight = 4,
                        backgroundColor = isCurrent ? new Color(0.3f, 0.5f, 0.8f, 0.4f) : Color.clear,
                        borderBottomWidth = isCurrent ? 2 : 0,
                        borderBottomColor = new Color(0.4f, 0.6f, 0.9f),
                    }
                };

                if (content.image != null)
                {
                    btn.Add(new Image
                    {
                        image = content.image,
                        style = { width = 14, height = 14, flexShrink = 0, marginRight = 2 }
                    });
                }

                btn.Add(new Label(obj.name)
                {
                    style =
                    {
                        fontSize = 11,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        overflow = Overflow.Hidden,
                        textOverflow = TextOverflow.Ellipsis,
                        whiteSpace = WhiteSpace.NoWrap,
                        flexShrink = 1,
                        maxWidth = 100,
                        color = isCurrent ? Color.white : new Color(0.78f, 0.78f, 0.78f),
                    }
                });

                container.Add(btn);
            }

            // Ellipsis at end
            if (end < history.Count - 1)
            {
                container.Add(new Label("...")
                {
                    style =
                    {
                        color = new Color(0.5f, 0.5f, 0.5f),
                        unityTextAlign = TextAnchor.MiddleCenter,
                        width = 16,
                        marginLeft = 0, marginRight = 0,
                    }
                });
            }

            // Spacer
            container.Add(new VisualElement { style = { flexGrow = 1 } });

            // History menu button
            var menuBtn = new Button(ShowMenu)
            {
                text = "\u2630",
                tooltip = "History",
                style =
                {
                    width = 24,
                    marginLeft = 0, marginRight = 0,
                    paddingLeft = 0, paddingRight = 0,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            container.Add(menuBtn);
        }

        private void ShowMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("View All History"), false, OpenSearchWindow);
            menu.AddItem(new GUIContent("Clear History"), false, tracker.ClearHistory);
            menu.ShowAsContext();
        }

        private void OpenSearchWindow()
        {
            var history = tracker.History;
            var dic = new System.Collections.Generic.Dictionary<string, int>();

            for (int i = history.Count - 1; i >= 0; i--)
            {
                var obj = history[i];
                if (obj == null) continue;
                var key = $"{obj.name} ({obj.GetType().Name})";
                if (!dic.ContainsKey(key))
                    dic[key] = i;
            }

            SearchWindow.Show(dic.Keys, str =>
            {
                tracker.NavigateTo(dic[str]);
            });
        }
    }
}
#endif

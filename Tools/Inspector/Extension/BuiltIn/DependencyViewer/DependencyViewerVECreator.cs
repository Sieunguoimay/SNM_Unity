#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public static class DependencyViewerVECreator
    {
        private static readonly Color SectionBg = new(0f, 0f, 0f, 0.15f);
        private static readonly Color HeaderColor = new(0.7f, 0.7f, 0.7f);
        private static readonly Color CountColor = new(0.5f, 0.5f, 0.5f);
        private static readonly Color HoverBg = new(1f, 1f, 1f, 0.06f);

        public static VisualElement BuildVE(Object[] targets)
        {
            if (targets == null || targets.Length == 0 || targets[0] == null)
                return new VisualElement();

            var target = targets[0];
            var assetPath = AssetDatabase.GetAssetPath(target);

            if (string.IsNullOrEmpty(assetPath))
                return new VisualElement();

            var outgoing = AssetDatabase.GetDependencies(assetPath, false)
                .Where(d => d != assetPath)
                .OrderBy(d => d)
                .ToList();

            var incoming = FindIncomingReferences(assetPath);

            if (outgoing.Count == 0 && incoming.Count == 0)
                return new VisualElement();

            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 6,
                    marginBottom = 4,
                    paddingLeft = 2,
                    paddingRight = 2,
                }
            };

            if (outgoing.Count > 0)
                root.Add(BuildColumn("References", outgoing));

            if (incoming.Count > 0)
                root.Add(BuildColumn("Referenced By", incoming));

            return root;
        }

        private static List<string> FindIncomingReferences(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return new List<string>();

            var entries = Snm.Tools.YamlIndexDatabase.GetIncomingReferences(guid);
            var result = entries
                .Select(e => e.path)
                .Where(p => !string.IsNullOrEmpty(p) && p != assetPath)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            return result;
        }

        private const int MaxVisibleItems = 5;

        private static VisualElement BuildColumn(string title, List<string> paths)
        {
            var column = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = 0,
                    marginLeft = 2,
                    marginRight = 2,
                    backgroundColor = SectionBg,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                }
            };

            // Header
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0f, 0f, 0f, 0.2f),
                }
            };

            header.Add(new Label(title)
            {
                style =
                {
                    color = HeaderColor,
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            });

            header.Add(new Label(paths.Count.ToString())
            {
                style =
                {
                    color = CountColor,
                    fontSize = 11,
                }
            });

            column.Add(header);

            // Items — show up to MaxVisibleItems, hide the rest behind a "Show more" button
            var overflow = new VisualElement { style = { display = DisplayStyle.None } };

            for (int i = 0; i < paths.Count; i++)
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(paths[i]);
                if (obj == null) continue;

                var item = BuildItem(obj, paths[i]);
                if (i < MaxVisibleItems)
                    column.Add(item);
                else
                    overflow.Add(item);
            }

            if (paths.Count > MaxVisibleItems)
            {
                var showMoreBtn = new Label($"Show {paths.Count - MaxVisibleItems} more…")
                {
                    style =
                    {
                        color = CountColor,
                        fontSize = 11,
                        paddingLeft = 8,
                        paddingTop = 4,
                        paddingBottom = 4,
                        unityFontStyleAndWeight = FontStyle.Italic,
                    }
                };

                showMoreBtn.RegisterCallback<MouseEnterEvent>(_ => showMoreBtn.style.color = HeaderColor);
                showMoreBtn.RegisterCallback<MouseLeaveEvent>(_ => showMoreBtn.style.color = CountColor);
                showMoreBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    overflow.style.display = DisplayStyle.Flex;
                    showMoreBtn.style.display = DisplayStyle.None;
                });

                column.Add(showMoreBtn);
                column.Add(overflow);
            }

            return column;
        }

        private static VisualElement BuildItem(Object obj, string path)
        {
            var icon = AssetDatabase.GetCachedIcon(path) as Texture2D;

            var row = new VisualElement
            {
                tooltip = path,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 8,
                    paddingRight = 4,
                }
            };

            row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = HoverBg);
            row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);

            var capturedObj = obj;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount >= 2)
                    Selection.activeObject = capturedObj;
                else
                    EditorGUIUtility.PingObject(capturedObj);
            });

            if (icon != null)
            {
                row.Add(new Image
                {
                    image = icon,
                    style = { width = 14, height = 14, marginRight = 6 }
                });
            }

            row.Add(new Label(obj.name)
            {
                style =
                {
                    fontSize = 11,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    whiteSpace = WhiteSpace.NoWrap,
                    flexShrink = 1,
                }
            });

            return row;
        }
    }
}
#endif

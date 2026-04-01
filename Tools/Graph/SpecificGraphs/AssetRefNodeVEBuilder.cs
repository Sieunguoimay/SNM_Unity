#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public class AssetRefNodeVEBuilder : INodeVEBuilder
    {
        private readonly Func<Node, UnityEngine.Object> assetResolver;

        public AssetRefNodeVEBuilder(Func<Node, UnityEngine.Object> assetResolver)
        {
            this.assetResolver = assetResolver;
        }

        public VisualElement CreateNodeVE(Node node, Action<Port, VisualElement> createPortVECallback)
        {
            var asset = assetResolver(node);
            var ve = CreateProjectItem(asset);
            ve.name = $"node-{node.id}";
            ve.style.position = Position.Absolute;
            ve.style.left = node.position.x;
            ve.style.top = node.position.y;
            return ve;
        }

        public static VisualElement CreateProjectItem(UnityEngine.Object asset)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                }
            };

            var icon = asset != null ? EditorGUIUtility.ObjectContent(asset, asset.GetType()).image : null;
            var iconElement = new Image { image = icon, style = { width = 16, height = 16 } };
            var label = new Label { text = asset?.name ?? "null", style = { marginLeft = 4 } };

            iconElement.RegisterCallback<PointerDownEvent>(evt =>
            {
                EditorGUIUtility.PingObject(asset);
                evt.StopPropagation();
            });

            container.Add(iconElement);
            container.Add(label);

            return container;
        }
    }
}
#endif
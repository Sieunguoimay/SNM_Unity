#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
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
}
#endif
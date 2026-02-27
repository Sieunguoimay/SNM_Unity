#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class ContextMenuListVEBuilder
    {
        public static VisualElement BuildVE(UnityEngine.Object target)
        {
            var root = new VisualElement();

            foreach (var (method, menuAttr) in ContextMenuHelper.GetMethodInfos(target))
            {
                var button = new Button
                {
                    text = menuAttr.menuItem,
                    clickable = new(() => ContextMenuHelper.InvokeMethod(method, target))
                };

                root.Add(button);
            }
            return root;
        }
    }
}
#endif

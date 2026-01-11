using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugToolVECreator
    {
        public static VisualElement Create(GrassDebugTool tool)
        {
            var root = new VisualElement();
            var button_ShowWindMap = new Button() { text = "Toggle Wind Map", clickable = new(tool.ToggleWindMap) };

            root.Add(button_ShowWindMap);
            return root;
        }
    }
}
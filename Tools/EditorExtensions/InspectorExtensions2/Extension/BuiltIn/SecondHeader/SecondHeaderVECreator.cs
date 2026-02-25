#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class SecondHeaderVECreator
    {
        public static VisualElement Create(SerializedObject serializedObject, VisualElement imguiContainer)
        {
            if (serializedObject.targetObjects.Length > 1) return new VisualElement();

            var target = serializedObject.targetObject;
            var root = new VisualElement();
            var layout_Buttons = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.RowReverse,
                    flexGrow = 1,
                    marginBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, .2f),
                    height = EditorGUIUtility.singleLineHeight + 2,
                }
            };

            var button_Browse = new Button { text = "Browse", clickable = new(() => SecondHeaderTools.OpenObjectBrowser(target)) };
            var button_EditScript = new Button() { text = "Edit Script", clickable = new(() => SecondHeaderTools.OpenScript(target)), };
            var button_Ping = new Button() { text = "Ping", clickable = new(() => EditorGUIUtility.PingObject(target)) };
            var button_Open = new Button() { text = "To Window", clickable = new(() => EditorPopupWindow.Open(target)), tooltip = "Open in new Window", };
            var layout_Refs = new VisualElement();
            var button_Toggle = CustomEditorVECreator.BuildVE(serializedObject, imguiContainer, layout_Refs);
            var button_Find = CreateFindReferences(target);
            var shouldShow_EditScript = target is MonoBehaviour || target is ScriptableObject;

            if (shouldShow_EditScript) layout_Buttons.Add(button_EditScript);
            layout_Buttons.Add(button_Ping);
            layout_Buttons.Add(button_Browse);
            layout_Buttons.Add(button_Open);
            layout_Buttons.Add(button_Find);
            layout_Buttons.Add(button_Toggle);

            root.Add(layout_Buttons);
            root.Add(layout_Refs);

            return root;
        }

        private static VisualElement CreateFindReferences(Object target)
        {
            var button = new Button()
            {
                text = "Find",
                clickable = new(() =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Find Scene References"), false, () => SecondHeaderTools.OpenFindReferencesInScene(target));
                    menu.AddItem(new GUIContent("Find Asset References"), false, () => SecondHeaderTools.FindRefrencesInProject(target));
                    menu.ShowAsContext();
                }),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image)
#endif
            };
#if UNITY_2023_2_OR_NEWER
            var image = button.Q<Image>();
            image.style.height = 17;
            image.style.width = 17;
#endif
            return button;
        }
    }
}
#endif

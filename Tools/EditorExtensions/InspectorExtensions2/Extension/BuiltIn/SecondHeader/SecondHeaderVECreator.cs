#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class SecondHeaderVECreator
    {
        public static VisualElement Create(SerializedObject serializedObject)
        {
            var target = serializedObject.targetObject;
            var assetPath = AssetDatabase.GetAssetPath(target);
            var debugMode = new InspectorMode(serializedObject);

            var root = new VisualElement()
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
            var button_Open = new Button()
            {
                tooltip = "Open in new Window",
                text = "Open",
                clickable = new(() => EditorPopupWindow.Open(target)),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_ScaleTool").image)
#endif
            };
            var button_Find = CreateFindReferences(target);
            var shouldShow_EditScript = target is MonoBehaviour || target is ScriptableObject;

            if (shouldShow_EditScript) root.Add(button_EditScript);
            root.Add(button_Ping);
            root.Add(button_Browse);
            root.Add(button_Open);
            root.Add(button_Find);

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
                    menu.AddItem(new GUIContent("Find in Scene"), false, () => SecondHeaderTools.OpenFindReferencesInScene(target));
                    menu.AddItem(new GUIContent("Find in Project"), false, () => SecondHeaderTools.FindRefrencesInProject(target));
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
